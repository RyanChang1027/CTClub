using System.Web.Mvc;
using CrownTowersClub.Infrastructure.DependencyInjection;

namespace CrownTowersClub.App_Start
{
    public static class DependencyConfig
    {
        public static void RegisterDependencies()
        {
            DependencyResolver.SetResolver(
                new SimpleDependencyResolver());
        }
    }
}
