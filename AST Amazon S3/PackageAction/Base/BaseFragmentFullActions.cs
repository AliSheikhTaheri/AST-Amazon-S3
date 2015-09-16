namespace AST.S3.PackageAction.Base
{
    using System;
    using System.Web;
    using System.Xml;

    using PackageActionsContrib.Helpers;

    public class BaseFragmentFullActions
    {
        protected bool AddXmlFragment(XmlNode xmlData, string attributeName)
        {
            // The config file we want to modify
            var configFileName = VirtualPathUtility.ToAbsolute(XmlHelper.GetAttributeValueFromNode(xmlData, "file"));

            // Xpath expression to determine the rootnode
            var xPath = XmlHelper.GetAttributeValueFromNode(xmlData, attributeName);

            // Holds the position where we want to insert the xml Fragment
            var position = XmlHelper.GetAttributeValueFromNode(xmlData, "position", "end");

            // Open the config file
            var configDocument = Umbraco.Core.XmlHelper.OpenAsXmlDocument(configFileName);

            // The xml fragment we want to insert
            var xmlFragment = xmlData.SelectSingleNode("./*");

            // Select rootnode using the xpath
            var rootNode = configDocument.SelectSingleNode(xPath);

            if (rootNode != null && xmlFragment != null)
            {
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
            }

            return true;
        }

        protected bool RemoveXmlFragment(XmlNode xmlData, string attributeName)
        {
            // The config file we want to modify
            var configFileName = VirtualPathUtility.ToAbsolute(XmlHelper.GetAttributeValueFromNode(xmlData, "file"));

            // Xpath expression to determine the rootnode
            var xPath = XmlHelper.GetAttributeValueFromNode(xmlData, attributeName);

            // Open the config file
            var configDocument = Umbraco.Core.XmlHelper.OpenAsXmlDocument(configFileName);

            // Select the node to remove using the xpath
            var node = configDocument.SelectSingleNode(xPath);

            // Remove the node 
            node?.ParentNode?.RemoveChild(node);

            // Save the modified document
            configDocument.Save(HttpContext.Current.Server.MapPath(configFileName));

            return true;
        }
    }
}
