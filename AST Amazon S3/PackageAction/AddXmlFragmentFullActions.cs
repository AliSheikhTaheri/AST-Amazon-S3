namespace AST.S3.PackageAction
{
    using System;
    using System.Web;
    using System.Xml;

    using PackageActionsContrib.Helpers;

    using umbraco.cms.businesslogic.packager.standardPackageActions;
    using umbraco.interfaces;

    public class AddXmlFragmentFullActions : IPackageAction
    {
        #region Package action methods

        public string Alias()
        {
            return "AddXmlFragmentFullActions";
        }

        public bool Execute(string packageName, XmlNode xmlData)
        {
            return AddXmlFragment(xmlData);
        }

        public bool Undo(string packageName, XmlNode xmlData)
        {
            return RemoveXmlFragment(xmlData);
        }

        public XmlNode SampleXml()
        {
            string sample = "<Action runat=\"install\" undo=\"true/false\" alias=\"AddXmlFragmentFullActions\" file=\"~/config/umbracosettings.config\" xpathInstall=\"//help\" xpathUninstall=\"//help/link[@application='content']\" position=\"end\"><link application=\"content\" applicationUrl=\"dashboard.aspx\"  language=\"en\" userType=\"Administrators\" helpUrl=\"http://www.xyz.no?{0}/{1}/{2}/{3}\" /></Action>";
            return helper.parseStringToXmlNode(sample);
        }

        #endregion

        #region Helpers

        private static bool AddXmlFragment(XmlNode xmlData)
        {
            // The config file we want to modify
            string configFileName = VirtualPathUtility.ToAbsolute(XmlHelper.GetAttributeValueFromNode(xmlData, "file"));

            // Xpath expression to determine the rootnode
            string xPath = XmlHelper.GetAttributeValueFromNode(xmlData, "xpathInstall");

            // Holds the position where we want to insert the xml Fragment
            string position = XmlHelper.GetAttributeValueFromNode(xmlData, "position", "end");

            // Open the config file
            XmlDocument configDocument = Umbraco.Core.XmlHelper.OpenAsXmlDocument(configFileName);

            // The xml fragment we want to insert
            XmlNode xmlFragment = xmlData.SelectSingleNode("./*");

            // Select rootnode using the xpath
            XmlNode rootNode = configDocument.SelectSingleNode(xPath);

            if (position.Equals("beginning", StringComparison.CurrentCultureIgnoreCase))
            {
                // Add xml fragment to the beginning of the selected rootnode
                rootNode.PrependChild(configDocument.ImportNode(xmlFragment, true));
            }
            else
            {
                // add xml fragment to the end of the selected rootnode
                rootNode.AppendChild(configDocument.ImportNode(xmlFragment, true));
            }

            // Save the modified document
            configDocument.Save(HttpContext.Current.Server.MapPath(configFileName));

            return true;
        }

        private static bool RemoveXmlFragment(XmlNode xmlData)
        {
            // The config file we want to modify
            string configFileName = VirtualPathUtility.ToAbsolute(XmlHelper.GetAttributeValueFromNode(xmlData, "file"));

            // Xpath expression to determine the rootnode
            string xPath = XmlHelper.GetAttributeValueFromNode(xmlData, "xpathUninstall");

            // Open the config file
            XmlDocument configDocument = Umbraco.Core.XmlHelper.OpenAsXmlDocument(configFileName);

            // Select the node to remove using the xpath
            XmlNode node = configDocument.SelectSingleNode(xPath);

            // Remove the node 
            if (node != null)
            {
                node.ParentNode.RemoveChild(node);
            }

            // Save the modified document
            configDocument.Save(HttpContext.Current.Server.MapPath(configFileName));

            return true;
        }

        #endregion
    }
}
