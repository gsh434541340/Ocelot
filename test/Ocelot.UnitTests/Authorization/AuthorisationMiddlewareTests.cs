﻿using Ocelot.Middleware;

namespace Ocelot.UnitTests.Authorization
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using Moq;
    using Ocelot.Authorisation;
    using Ocelot.Authorisation.Middleware;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Middleware;

    public class AuthorisationMiddlewareTests
    {
        private readonly Mock<IClaimsAuthoriser> _authService;
        private readonly Mock<IScopesAuthoriser> _authScopesService;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly AuthorisationMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public AuthorisationMiddlewareTests()
        {
            _authService = new Mock<IClaimsAuthoriser>();
            _authScopesService = new Mock<IScopesAuthoriser>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<AuthorisationMiddleware>()).Returns(_logger.Object);
            _next = async context => {
                //do nothing
            };
            _middleware = new AuthorisationMiddleware(_next, _authService.Object, _authScopesService.Object, _loggerFactory.Object);
        }

        [Fact]
        public void should_call_authorisation_service()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<PlaceholderNameAndValue>(), 
                new ReRouteBuilder()
                    .WithIsAuthorised(true)
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build())))
                .And(x => x.GivenTheAuthServiceReturns(new OkResponse<bool>(true)))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAuthServiceIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamContext.TemplatePlaceholderNameAndValues = downstreamRoute.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }

        private void GivenTheAuthServiceReturns(Response<bool> expected)
        {
            _authService
                .Setup(x => x.Authorise(It.IsAny<ClaimsPrincipal>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(expected);
        }

        private void ThenTheAuthServiceIsCalledCorrectly()
        {
            _authService
                .Verify(x => x.Authorise(It.IsAny<ClaimsPrincipal>(),
                It.IsAny<Dictionary<string, string>>()), Times.Once);
        }
    }
}
