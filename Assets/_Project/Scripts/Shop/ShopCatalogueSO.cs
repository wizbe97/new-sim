// using System.Collections.Generic;
// using Project.UI.Phone;
// using UnityEngine;
// using Object = UnityEngine.Object;

// namespace Project.Shop
// {
//     [CreateAssetMenu(
//         fileName = "ShopCatalogue",
//         menuName = "Project/Shop/Shop Catalogue")]
//     public sealed class ShopCatalogueSO : PhonePageCatalogueSO
//     {
//         [Header("Catalogue Items")]
//         [SerializeField] private List<StoreItemSO> items = new();

//         public IReadOnlyList<StoreItemSO> Items => items;

//         public override int ItemCount => items.Count;

//         public override Object GetItem(int index)
//         {
//             if (index < 0 || index >= items.Count)
//             {
//                 return null;
//             }

//             return items[index];
//         }

//         public bool Contains(StoreItemSO item)
//         {
//             return item != null && items.Contains(item);
//         }

// #if UNITY_EDITOR
//         protected override void OnValidate()
//         {
//             base.OnValidate();

//             items.RemoveAll(item => item == null);

//             HashSet<StoreItemSO> uniqueItems = new();

//             for (int i = items.Count - 1; i >= 0; i--)
//             {
//                 StoreItemSO item = items[i];

//                 if (!uniqueItems.Add(item))
//                 {
//                     Debug.LogWarning(
//                         $"{nameof(ShopCatalogueSO)} '{name}' contains duplicate item '{item.name}'. Removing duplicate.",
//                         this
//                     );

//                     items.RemoveAt(i);
//                 }
//             }
//         }
// #endif
//     }
// }