using UnityEngine;

namespace Project.UI.Phone
{
    public interface IPhoneCatalogueItemView
    {
        void Initialize(Object item, PhonePageContext context);
        void Refresh();
        void Dispose();
    }
}