using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Models;


namespace Shiakati.Models
{
    public partial class CartItem : ObservableObject
    {
        public ProductVariantsModel Variant { get; }
        public ProductModel Product { get; }
        

        // Prix total SANS aucune remise (SalePrice * Quantité)
        public decimal RawTotal => Variant.SalePrice * Quantity;

        // Montant total économisé sur CETTE ligne (RemiseFixe + RemiseManuelle) * Quantité
        public decimal TotalLineDiscount
        {
            get
            {
                decimal unitDiscount = 0;

                if (IsDiscountPinned && Variant.DiscountFixed.HasValue)
                    unitDiscount += Variant.DiscountFixed.Value;

                unitDiscount += ManualDiscount;

                // On s'assure de ne pas dépasser le prix de l'article
                if (unitDiscount > Variant.SalePrice) unitDiscount = Variant.SalePrice;

                return unitDiscount * Quantity;
            }
        }
        public decimal TotalPrice => RawTotal - TotalLineDiscount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))]
        [NotifyPropertyChangedFor(nameof(RawTotal))]
        [NotifyPropertyChangedFor(nameof(TotalLineDiscount))]
        private int _quantity;

        [ObservableProperty]
        private bool _isDiscountPinned = false;

        // NOUVEAU : Propriété pour la remise manuelle
        [ObservableProperty]
        private decimal _manualDiscount;

        // MAGIE DU TOOLKIT : Cette méthode est appelée automatiquement 
        // à chaque fois que l'utilisateur tape un chiffre dans la case ManualDiscount
        partial void OnManualDiscountChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalLineDiscount));
        }        

        public string DisplayName => $"{Product?.ProductName} {Variant?.FullSize} {Variant?.Color}".Trim();

        public CartItem(ProductVariantsModel variant, ProductModel product)
        {
            Variant = variant;
            Product = product;
            _quantity = 1;
            
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

