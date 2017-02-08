using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Events.Hooks;
using Sitecore.SecurityModel;

namespace Sitecore.Support.Hooks
{
    public class UpdateFullPageXHtmlValidator: IHook
    {
        public void Initialize()
        {
            using (new SecurityDisabler())
            {
                var databaseName = "master";
                var itemPath = "/sitecore/system/Settings/Validation Rules/Item Rules/Item/Full Page XHtml";
                var fieldName = "Type";

                // protects from refactoring-related mistakes
                var type = typeof(Sitecore.Support.Data.Validators.ItemValidators.FullPageXHtmlValidator);

                var typeName = type.FullName;
                var assemblyName = type.Assembly.GetName().Name;
                var fieldValue = $"{typeName}, {assemblyName}";

                var database = Factory.GetDatabase(databaseName);
                var item = database.GetItem(itemPath);

                if (string.Equals(item[fieldName], fieldValue, StringComparison.Ordinal))
                {
                    // already installed
                    return;
                }

                Log.Info($"Installing {assemblyName}", this);
                item.Editing.BeginEdit();
                item[fieldName] = fieldValue;
                item.Editing.EndEdit();
            }
        }
    }
}