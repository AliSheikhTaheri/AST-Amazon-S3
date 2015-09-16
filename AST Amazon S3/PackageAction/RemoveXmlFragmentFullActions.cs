namespace AST.S3.PackageAction
{
    using System.Xml;

    using AST.S3.PackageAction.Base;

    using umbraco.cms.businesslogic.packager.standardPackageActions;
    using umbraco.interfaces;

    public class RemoveXmlFragmentFullActions : BaseFragmentFullActions, IPackageAction
    {
        #region Package action methods

        public string Alias() => "RemoveXmlFragmentFullActions";

        public bool Undo(string packageName, XmlNode xmlData) => AddXmlFragment(xmlData, "xpathUninstall");

        public bool Execute(string packageName, XmlNode xmlData) => RemoveXmlFragment(xmlData, "xpathInstall");

        public XmlNode SampleXml() => helper.parseStringToXmlNode("<Action runat=\"install\" undo=\"false\" alias=\"RemoveXmlFragmentFullActions\" file=\"~/config/umbracosettings.config\" xpathInstall=\"//help/link[@application='content']\" xpathUninstall=\"//help/link[@application='content']\" position=\"end\"><link application=\"content\" applicationUrl=\"dashboard.aspx\"  language=\"en\" userType=\"Administrators\" helpUrl=\"http://www.xyz.no?{0}/{1}/{2}/{3}\" />");

        #endregion
    }
}
