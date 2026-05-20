using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Models;

namespace Shiakati.Models
{
    public partial class CartItem : ObservableObject
    {
        public ProductVariantModel? Variant { get; }
        public ProductModel? Product { get; }

        // Prix total SANS aucune remise (SalePrice * Quantité)
        public decimal? RawTotal => (Variant?.SalePrice ?? 0) * (Quantity ?? 0);

        // Montant total économisé sur CETTE ligne (RemiseFixe + RemiseManuelle) * Quantité
        public decimal? TotalLineDiscount
        {
            get
            {
                decimal unitDiscount = 0;

                if (Variant == null) return 0;

                if (IsDiscountPinned && Variant.DiscountFixed.HasValue)
                    unitDiscount += Variant.DiscountFixed.Value;

                if (ManualDiscount.HasValue)
                    unitDiscount += ManualDiscount.Value;

                // On s'assure de ne pas dépasser le prix de l'article
                if (unitDiscount > (Variant.SalePrice ?? 0))
                    unitDiscount = Variant.SalePrice ?? 0;

                return unitDiscount * (Quantity ?? 0);
            }
        }

        public decimal? TotalPrice => RawTotal - TotalLineDiscount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))]
        [NotifyPropertyChangedFor(nameof(RawTotal))]
        [NotifyPropertyChangedFor(nameof(TotalLineDiscount))]
        private int? _quantity;

        [ObservableProperty]
        private bool _isDiscountPinned = false;

        [ObservableProperty]
        private decimal? _manualDiscount;

        partial void OnManualDiscountChanged(decimal? value)
        {
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalLineDiscount));
        }

        // Priorise les données dénormalisées du Variant, sinon fallback sur l'objet Product
        public string DisplayName => $"{Variant?.ProductName ?? Product?.ProductName} {Variant?.FullSize} {Variant?.Color}".Trim();

        // Constructeur flexible : accepte un Variant seul ou avec son Produit parent
        public CartItem(ProductVariantModel variant, ProductModel? product = null)
        {
            Variant = variant;
            Product = product;
            Quantity = 1;
        }

        [RelayCommand]
        private void ToggleDiscount()
        {
            IsDiscountPinned = !IsDiscountPinned;
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalLineDiscount));
        }
    }
}
