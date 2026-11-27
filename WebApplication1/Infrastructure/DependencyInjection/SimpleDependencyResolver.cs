using CrownTowersClub.Controllers;
using CrownTowersClub.Infrastructure.Data;
using CrownTowersClub.Repositories;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using CrownTowersClub.Services;

namespace CrownTowersClub.Infrastructure.DependencyInjection
{
    internal sealed class SimpleDependencyResolver
        : IDependencyResolver
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(MemberController))
            {
                IDbConnectionFactory connectionFactory =
                    new SqlConnectionFactory();

                return new MemberController(
                    new MemberRepository(
                        connectionFactory),
                    new CorporateEmployeeRepository(
                        connectionFactory));
            }

            if (serviceType == typeof(DevSessionController))
            {
                IDbConnectionFactory connectionFactory =
                    new SqlConnectionFactory();

                return new DevSessionController(
                    new CorporateEmployeeRepository(connectionFactory));
            }

            if (serviceType == typeof(ReviewController))
            {
                IDbConnectionFactory connectionFactory =
                    new SqlConnectionFactory();

                return new ReviewController(
                    new MemberRepository(connectionFactory));
            }

            if (serviceType == typeof(HomeController))
            {
                IDbConnectionFactory connectionFactory = new SqlConnectionFactory();
                return new HomeController(
                    new MemberRepository(connectionFactory));
            }

            if (serviceType == typeof(EventsController))
            {
                IDbConnectionFactory connectionFactory =
                    new SqlConnectionFactory();

                return new EventsController(
                    new EventRepository(connectionFactory),
                    new VenueConfigurationProvider(),
                    new MemberRepository(connectionFactory));
            }

            if (serviceType == typeof(IEventRepository))
            {
                return new EventRepository(
                    new SqlConnectionFactory());
            }

            if (serviceType == typeof(IMemberRepository))
            {
                return new MemberRepository(
                    new SqlConnectionFactory());
            }

            if (serviceType ==
                typeof(ICorporateEmployeeRepository))
            {
                return new CorporateEmployeeRepository(
                    new SqlConnectionFactory());
            }

            if (serviceType ==
                typeof(IDbConnectionFactory))
            {
                return new SqlConnectionFactory();
            }

            return null;
        }

        public IEnumerable<object> GetServices(
            Type serviceType)
        {
            return new object[0];
        }
    }
}