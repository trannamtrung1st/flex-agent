using System.Net;
using System.Security.Cryptography;
using System.Text;
using DotNet.Testcontainers.Builders;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure.ObjectStorage;

namespace FlexAgent.Artifact.Integration.Tests;

[Collection(nameof(ArtifactCollection))]
public sealed class SeaweedFsArtifactContractTests(ArtifactIntegrationFixture fixture)
{
  [Fact]
  public async Task Put_get_presign_and_conditional_create_against_seaweedfs()
  {
    var cancellationToken = TestContext.Current.CancellationToken;
    var organizationId = Guid.NewGuid();
    var artifactId = Guid.NewGuid();
    var key = ArtifactObjectKey.Create(organizationId, artifactId);
    var content = Encoding.UTF8.GetBytes("synthetic submission text for artifact gate");

    var put = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain",
      ConditionalCreate: true), cancellationToken);
    Assert.True(put.Succeeded, put.OutcomeCode);
    Assert.NotNull(put.Reference);

    var duplicate = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain",
      ConditionalCreate: true), cancellationToken);
    Assert.False(duplicate.Succeeded);
    Assert.Equal(ArtifactOutcomeCodes.AlreadyExists, duplicate.OutcomeCode);

    var get = await fixture.Store.GetExactVersionAsync(new ArtifactGetRequest(
      organizationId,
      put.Reference!), cancellationToken);
    Assert.True(get.Succeeded, get.OutcomeCode);
    Assert.Equal(content, get.Content.ToArray());

    var presign = await fixture.Store.IssueDownloadCapabilityAsync(new ArtifactPresignRequest(
      organizationId,
      Guid.NewGuid(),
      "download",
      key,
      TimeSpan.FromMinutes(1)), cancellationToken);
    Assert.True(presign.Succeeded, presign.OutcomeCode);
    Assert.NotNull(presign.PresignedUrl);

    using var http = new HttpClient();
    using var response = await http.GetAsync(presign.PresignedUrl, cancellationToken);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var downloaded = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    Assert.Equal(ComputeDigest(content), ComputeDigest(downloaded));
  }

  [Fact]
  public async Task Get_rejects_wrong_organization_scope_without_reading_object()
  {
    var cancellationToken = TestContext.Current.CancellationToken;
    var organizationId = Guid.NewGuid();
    var otherOrganizationId = Guid.NewGuid();
    var key = ArtifactObjectKey.Create(organizationId, Guid.NewGuid());
    var content = Encoding.UTF8.GetBytes("scope isolation check");

    var put = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain"), cancellationToken);
    Assert.True(put.Succeeded, put.OutcomeCode);

    var get = await fixture.Store.GetExactVersionAsync(new ArtifactGetRequest(
      otherOrganizationId,
      put.Reference!), cancellationToken);
    Assert.False(get.Succeeded);
    Assert.Equal(ArtifactOutcomeCodes.ScopeMismatch, get.OutcomeCode);
  }

  [Fact]
  public async Task Put_rejects_object_key_scoped_to_another_organization()
  {
    var cancellationToken = TestContext.Current.CancellationToken;
    var organizationId = Guid.NewGuid();
    var otherOrganizationId = Guid.NewGuid();
    var key = ArtifactObjectKey.Create(otherOrganizationId, Guid.NewGuid());
    var content = Encoding.UTF8.GetBytes("put scope isolation");

    var put = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain"), cancellationToken);
    Assert.False(put.Succeeded);
    Assert.Equal(ArtifactOutcomeCodes.ScopeMismatch, put.OutcomeCode);
  }

  [Fact]
  public async Task Delete_rejects_wrong_organization_scope()
  {
    var cancellationToken = TestContext.Current.CancellationToken;
    var organizationId = Guid.NewGuid();
    var otherOrganizationId = Guid.NewGuid();
    var key = ArtifactObjectKey.Create(organizationId, Guid.NewGuid());
    var content = Encoding.UTF8.GetBytes("delete scope isolation");

    var put = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain"), cancellationToken);
    Assert.True(put.Succeeded, put.OutcomeCode);

    var deleted = await fixture.Store.DeleteAsync(otherOrganizationId, put.Reference!, cancellationToken);
    Assert.False(deleted);
  }

  [Fact]
  public async Task Upload_presign_rejects_wrong_organization_scope()
  {
    var cancellationToken = TestContext.Current.CancellationToken;
    var organizationId = Guid.NewGuid();
    var otherOrganizationId = Guid.NewGuid();
    var key = ArtifactObjectKey.Create(organizationId, Guid.NewGuid());

    var presign = await fixture.Store.IssueUploadCapabilityAsync(new ArtifactPresignRequest(
      otherOrganizationId,
      Guid.NewGuid(),
      "upload",
      key,
      TimeSpan.FromMinutes(1),
      ContentType: "text/plain"), cancellationToken);
    Assert.False(presign.Succeeded);
    Assert.Equal(ArtifactOutcomeCodes.ScopeMismatch, presign.OutcomeCode);
  }

  [Fact]
  public async Task Download_presign_rejects_wrong_organization_scope()
  {
    var cancellationToken = TestContext.Current.CancellationToken;
    var organizationId = Guid.NewGuid();
    var otherOrganizationId = Guid.NewGuid();
    var key = ArtifactObjectKey.Create(organizationId, Guid.NewGuid());
    var content = Encoding.UTF8.GetBytes("download presign scope isolation");

    var put = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain"), cancellationToken);
    Assert.True(put.Succeeded, put.OutcomeCode);

    var presign = await fixture.Store.IssueDownloadCapabilityAsync(new ArtifactPresignRequest(
      otherOrganizationId,
      Guid.NewGuid(),
      "download",
      key,
      TimeSpan.FromMinutes(1)), cancellationToken);
    Assert.False(presign.Succeeded);
    Assert.Equal(ArtifactOutcomeCodes.ScopeMismatch, presign.OutcomeCode);
  }

  [Fact]
  public async Task Delete_then_restore_same_digest_is_readable_and_incomplete_cleanup_removes_bytes()
  {
    var cancellationToken = TestContext.Current.CancellationToken;
    var organizationId = Guid.NewGuid();
    var artifactId = Guid.NewGuid();
    var key = ArtifactObjectKey.Create(organizationId, artifactId);
    var content = Encoding.UTF8.GetBytes("paired restore synthetic text");

    var put = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain",
      ConditionalCreate: true), cancellationToken);
    Assert.True(put.Succeeded, put.OutcomeCode);
    var digest = ComputeDigest(content);

    Assert.True(await fixture.Store.DeleteAsync(organizationId, put.Reference!, cancellationToken));

    var restored = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain",
      ConditionalCreate: true), cancellationToken);
    Assert.True(restored.Succeeded, restored.OutcomeCode);
    Assert.Equal(digest, restored.Reference!.Digest.Sha256Hex);

    var get = await fixture.Store.GetExactVersionAsync(new ArtifactGetRequest(
      organizationId,
      restored.Reference), cancellationToken);
    Assert.True(get.Succeeded, get.OutcomeCode);
    Assert.Equal(content, get.Content.ToArray());

    Assert.True(await fixture.Store.DeleteAsync(organizationId, restored.Reference, cancellationToken));
    var missing = await fixture.Store.GetExactVersionAsync(new ArtifactGetRequest(
      organizationId,
      restored.Reference), cancellationToken);
    Assert.False(missing.Succeeded);
  }

  [Fact]
  public async Task Download_presign_expires_within_configured_lifetime()
  {
    var cancellationToken = TestContext.Current.CancellationToken;
    var organizationId = Guid.NewGuid();
    var key = ArtifactObjectKey.Create(organizationId, Guid.NewGuid());
    var content = Encoding.UTF8.GetBytes("presign expiry synthetic text");
    var put = await fixture.Store.PutAsync(new ArtifactPutRequest(
      organizationId,
      key,
      content,
      "text/plain"), cancellationToken);
    Assert.True(put.Succeeded, put.OutcomeCode);

    var presign = await fixture.Store.IssueDownloadCapabilityAsync(new ArtifactPresignRequest(
      organizationId,
      Guid.NewGuid(),
      "download",
      key,
      TimeSpan.FromSeconds(1)), cancellationToken);
    Assert.True(presign.Succeeded, presign.OutcomeCode);
    Assert.True(presign.ExpiresAtUtc <= DateTimeOffset.UtcNow.Add(SubmissionLifecycleClocks.ProtectedCapabilityLifetime));

    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    using var http = new HttpClient();
    using var response = await http.GetAsync(presign.PresignedUrl, cancellationToken);
    Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
  }

  private static string ComputeDigest(byte[] content) =>
    Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}

[CollectionDefinition(nameof(ArtifactCollection))]
public sealed class ArtifactCollection : ICollectionFixture<ArtifactIntegrationFixture>;

public sealed class ArtifactIntegrationFixture : IAsyncLifetime
{
  private readonly DotNet.Testcontainers.Containers.IContainer _container;
  public S3ArtifactStore Store { get; private set; } = null!;

  public ArtifactIntegrationFixture()
  {
    var configPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "seaweedfs-s3.json");
    _container = new ContainerBuilder("chrislusf/seaweedfs:4.29")
      .WithPortBinding(8333, true)
      .WithPortBinding(9333, true)
      .WithBindMount(configPath, "/etc/seaweedfs/s3.json")
      .WithCommand("server", "-dir=/data", "-s3", "-s3.port=8333", "-s3.config=/etc/seaweedfs/s3.json")
      .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8333).ForPath("/status")))
      .Build();
  }

  public async ValueTask InitializeAsync()
  {
    try
    {
      await _container.StartAsync();
    }
    catch (Exception exception) when (IsDockerUnavailable(exception))
    {
      throw new InvalidOperationException($"Docker is unavailable: {exception.Message}");
    }

    var endpoint = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(8333)}";
    Store = new S3ArtifactStore(new S3ArtifactStoreOptions
    {
      ServiceUrl = endpoint,
      BucketName = "flex-agent-artifacts",
      AccessKeyId = "access_key",
      SecretAccessKey = "secret_key",
      ForcePathStyle = true,
    });

    await EnsureBucketAsync();
  }

  public async ValueTask DisposeAsync()
  {
    if (Store is IAsyncDisposable disposable)
    {
      await disposable.DisposeAsync();
    }

    await _container.DisposeAsync();
  }

  private async Task EnsureBucketAsync()
  {
    using var client = new Amazon.S3.AmazonS3Client(
      new Amazon.Runtime.BasicAWSCredentials("access_key", "secret_key"),
      new Amazon.S3.AmazonS3Config
      {
        ServiceURL = StoreEndpoint(),
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1",
      });

    try
    {
      await client.PutBucketAsync(new Amazon.S3.Model.PutBucketRequest
      {
        BucketName = BucketName(),
      }, TestContext.Current.CancellationToken);
    }
    catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
    {
    }
  }

  private string StoreEndpoint() =>
    $"http://{_container.Hostname}:{_container.GetMappedPublicPort(8333)}";

  private static string BucketName() => "flex-agent-artifacts";

  private static bool IsDockerUnavailable(Exception exception) =>
    exception.Message.Contains("docker", StringComparison.OrdinalIgnoreCase)
    || exception.Message.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase);
}
