using System.Web.Optimization;

namespace CrownTowersClub
{
    public static class BundleConfig
    {
        public static void RegisterBundles(
            BundleCollection bundles)
        {

            bundles.Add(new StyleBundle("~/bundles/vendor-css")
                .Include(
                    "~/Content/vendor/sweetalert2/sweetalert2.min.css"));

            bundles.Add(new StyleBundle("~/bundles/app-css")
                .Include(
                    "~/Content/app/membership-form.css",
                    "~/Content/app/review.css",
                    "~/Content/app/app-shell.css",
                    "~/Content/app/membership-card.css",
                    "~/Content/app/dashboard.css"));

            bundles.Add(new ScriptBundle("~/bundles/vendor-js")
                .Include(
                    "~/Scripts/vendor/jquery/jquery-3.7.1.min.js",
                    "~/Scripts/vendor/modernizr/modernizr-2.6.2.js",
                    "~/Scripts/vendor/sweetalert2/sweetalert2.all.min.js"));

            /*
             * Shared app shell: everything every page
             * may need.
             *
             * Order matters: base first, then
             * notifications, then the page-specific
             * module.
             */
            bundles.Add(new ScriptBundle("~/bundles/app-base")
                .Include(
                    "~/Scripts/app/app.base.js",
                    "~/Scripts/app/app.notifications.js"));

            /*
             * Membership form page only.
             */
            bundles.Add(new ScriptBundle("~/bundles/app-member")
                .Include(
                    "~/Scripts/app/member.api.js",
                    "~/Scripts/app/membership-form.js"));

            /*
             * Review pages only.
             */
            bundles.Add(new ScriptBundle("~/bundles/app-review")
                .Include(
                    "~/Scripts/app/review.js"));

            /*
            * Avatar dropdown.
            */
            bundles.Add(new ScriptBundle("~/bundles/app-shell")
                .Include(
                    "~/Scripts/app/app-shell.js"));
        }
    }
}
