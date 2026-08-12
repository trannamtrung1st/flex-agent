namespace FlexAgent.Sessions.Application
{
    internal sealed class FakeApplicationService;
}

namespace FlexAgent.Sessions.Infrastructure
{
    internal sealed class FakeInfrastructureRepository;
}

namespace FlexAgent.Sessions.Domain
{
    internal sealed class ViolatingDomainDependsOnApplication
    {
        public Application.FakeApplicationService Service => new();
    }

    internal sealed class ViolatingDomainDependsOnInfrastructure
    {
        public Infrastructure.FakeInfrastructureRepository Repository => new();
    }
}
