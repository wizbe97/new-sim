using System;
using Project.Managers;
using Project.Shop;
using Project.Staff;
using Project.UI;

namespace Project.UI.Phone
{
    public sealed class PhonePageContext
    {
        private readonly Func<StoreItemSO, bool> shopOwnershipProvider;
        private readonly Func<StoreItemSO, bool> shopUnlockProvider;
        private readonly Action<StoreItemSO> shopPurchaseRequester;

        private readonly Func<StaffMemberSO, bool> staffOwnershipProvider;
        private readonly Func<StaffMemberSO, bool> staffUnlockProvider;
        private readonly Action<StaffMemberSO> staffHireRequester;

        private readonly Action closePhoneRequester;

        public PhonePageContext(
            PhoneView phoneView,
            UIManager uiManager,
            Func<StoreItemSO, bool> shopOwnershipProvider,
            Func<StoreItemSO, bool> shopUnlockProvider,
            Action<StoreItemSO> shopPurchaseRequester,
            Func<StaffMemberSO, bool> staffOwnershipProvider,
            Func<StaffMemberSO, bool> staffUnlockProvider,
            Action<StaffMemberSO> staffHireRequester,
            Action closePhoneRequester)
        {
            PhoneView = phoneView;
            UIManager = uiManager;

            this.shopOwnershipProvider = shopOwnershipProvider;
            this.shopUnlockProvider = shopUnlockProvider;
            this.shopPurchaseRequester = shopPurchaseRequester;

            this.staffOwnershipProvider = staffOwnershipProvider;
            this.staffUnlockProvider = staffUnlockProvider;
            this.staffHireRequester = staffHireRequester;

            this.closePhoneRequester = closePhoneRequester;
        }

        public PhoneView PhoneView { get; }
        public UIManager UIManager { get; }

        public bool IsShopItemOwned(StoreItemSO storeItem)
        {
            return shopOwnershipProvider != null && shopOwnershipProvider.Invoke(storeItem);
        }

        public bool IsShopItemUnlocked(StoreItemSO storeItem)
        {
            return shopUnlockProvider == null || shopUnlockProvider.Invoke(storeItem);
        }

        public void RequestShopItemPurchase(StoreItemSO storeItem)
        {
            shopPurchaseRequester?.Invoke(storeItem);
        }

        public bool IsStaffMemberHired(StaffMemberSO staffMember)
        {
            return staffOwnershipProvider != null && staffOwnershipProvider.Invoke(staffMember);
        }

        public bool IsStaffMemberUnlocked(StaffMemberSO staffMember)
        {
            return staffUnlockProvider == null || staffUnlockProvider.Invoke(staffMember);
        }

        public void RequestStaffHire(StaffMemberSO staffMember)
        {
            staffHireRequester?.Invoke(staffMember);
        }

        public void RequestClosePhone()
        {
            closePhoneRequester?.Invoke();
        }
    }
}