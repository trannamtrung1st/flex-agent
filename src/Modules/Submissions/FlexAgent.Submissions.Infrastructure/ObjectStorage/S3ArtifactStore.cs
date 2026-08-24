using System.Net;
using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Submissions.Infrastructure.ObjectStorage;

public sealed class S3ArtifactStoreOptions
{
    public required string ServiceUrl { get; init; }

    public required string BucketName { get; init; }

    public required string AccessKeyId { get; init; }

    public required string SecretAccessKey { get; init; }

    public bool ForcePathStyle { get; init; } = true;
}

public sealed class S3ArtifactStore : IArtifactStore, IAsyncDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly string _serviceUrl;

    public S3ArtifactStore(S3ArtifactStoreOptions options)
    {
        _bucket = options.BucketName;
        _serviceUrl = options.ServiceUrl;
        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = options.ForcePathStyle,
            AuthenticationRegion = "us-east-1",
            UseHttp = options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
        };
        _client = new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
            config);
    }

    public async Task<ArtifactPutResult> PutAsync(ArtifactPutRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var put = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = request.ObjectKey.Value,
                InputStream = new MemoryStream(request.Content.ToArray()),
                ContentType = request.ContentType,
            };

            if (request.ConditionalCreate)
            {
                put.IfNoneMatch = "*";
            }

            var response = await _client.PutObjectAsync(put, cancellationToken);
            var digest = ComputeDigest(request.Content.Span);
            return new ArtifactPutResult(
                true,
                new StoredArtifactReference(
                    request.ObjectKey,
                    new ArtifactVersionId(response.VersionId ?? response.ETag ?? string.Empty),
                    digest,
                    request.Content.Length),
                ArtifactOutcomeCodes.Stored);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            return new ArtifactPutResult(false, null, ArtifactOutcomeCodes.AlreadyExists);
        }
        catch (AmazonS3Exception)
        {
            return new ArtifactPutResult(false, null, ArtifactOutcomeCodes.StorageUnavailable);
        }
    }

    public async Task<ArtifactGetResult> GetExactVersionAsync(ArtifactGetRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var get = new GetObjectRequest
            {
                BucketName = _bucket,
                Key = request.Reference.ObjectKey.Value,
                VersionId = string.IsNullOrWhiteSpace(request.Reference.VersionId.Value)
                    ? null
                    : request.Reference.VersionId.Value,
            };
            using var response = await _client.GetObjectAsync(get, cancellationToken);
            using var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, cancellationToken);
            var content = memory.ToArray();
            var digest = ComputeDigest(content);
            if (!string.Equals(digest.Sha256Hex, request.Reference.Digest.Sha256Hex, StringComparison.Ordinal))
            {
                return new ArtifactGetResult(false, ReadOnlyMemory<byte>.Empty, ArtifactOutcomeCodes.DigestMismatch);
            }

            return new ArtifactGetResult(true, content, ArtifactOutcomeCodes.Stored);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new ArtifactGetResult(false, ReadOnlyMemory<byte>.Empty, ArtifactOutcomeCodes.NotFound);
        }
        catch (AmazonS3Exception)
        {
            return new ArtifactGetResult(false, ReadOnlyMemory<byte>.Empty, ArtifactOutcomeCodes.StorageUnavailable);
        }
    }

    public Task<ArtifactPresignResult> IssueUploadCapabilityAsync(
        ArtifactPresignRequest request,
        CancellationToken cancellationToken = default) =>
        IssuePresignedAsync(request, HttpVerb.PUT, cancellationToken);

    public Task<ArtifactPresignResult> IssueDownloadCapabilityAsync(
        ArtifactPresignRequest request,
        CancellationToken cancellationToken = default) =>
        IssuePresignedAsync(request, HttpVerb.GET, cancellationToken);

    public async Task<bool> DeleteAsync(
        Guid organizationId,
        StoredArtifactReference reference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucket,
                Key = reference.ObjectKey.Value,
                VersionId = string.IsNullOrWhiteSpace(reference.VersionId.Value)
                    ? null
                    : reference.VersionId.Value,
            }, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception)
        {
            return false;
        }
    }

    private async Task<ArtifactPresignResult> IssuePresignedAsync(
        ArtifactPresignRequest request,
        HttpVerb verb,
        CancellationToken cancellationToken)
    {
        try
        {
            var presign = new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = request.ObjectKey.Value,
                Verb = verb,
                Expires = DateTime.UtcNow.Add(request.Lifetime),
                ContentType = request.ContentType,
            };

            if (request.MaxContentLength is long maxLength)
            {
                presign.Headers.ContentLength = maxLength;
            }

            var url = await _client.GetPreSignedURLAsync(presign);
            if (_serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url["https://".Length..];
            }

            return new ArtifactPresignResult(
                true,
                new Uri(url),
                DateTimeOffset.UtcNow.Add(request.Lifetime),
                ArtifactOutcomeCodes.Presigned);
        }
        catch (AmazonS3Exception)
        {
            return new ArtifactPresignResult(false, null, null, ArtifactOutcomeCodes.StorageUnavailable);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await Task.CompletedTask;
    }

    private static ArtifactDigest ComputeDigest(ReadOnlySpan<byte> content) =>
        ArtifactDigest.FromHex(Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant());
}
