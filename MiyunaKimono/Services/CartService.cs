// Services/CartService.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MiyunaKimono.Models;

namespace MiyunaKimono.Services
{
    public class CartService : INotifyPropertyChanged
    {
        private static readonly Lazy<CartService> _lazy = new(() => new CartService());
        public static CartService Instance => _lazy.Value;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public ObservableCollection<CartLine> Lines { get; } = new();

        private int _totalQuantity;
        public int TotalQuantity
        {
            get => _totalQuantity;
            private set { _totalQuantity = value; Raise(nameof(TotalQuantity)); Raise(nameof(HasItems)); }
        }
        public bool HasItems => TotalQuantity > 0;

        private CartService() { }

        public void Add(Product p, int qty)
        {
            if (p == null || qty <= 0) return;
            var line = Find(p);
            if (line == null) Lines.Add(new CartLine { Product = p, Quantity = qty });
            else line.Quantity += qty;
            Recalc();
        }

        public void SetQuantity(Product p, int qty)
        {
            if (p == null) return;
            var line = Find(p);
            if (line == null) return;

            if (qty <= 0) Lines.Remove(line);
            else line.Quantity = qty;

            Recalc();
        }

        public void Clear()
        {
            Lines.Clear();
            Recalc();
        }

        private CartLine Find(Product p)
        {
            foreach (var l in Lines)
                if (l.Product?.Id == p.Id) return l;
            return null;
        }

        private void Recalc()
        {
            int q = 0;
            foreach (var l in Lines) q += l.Quantity;
            TotalQuantity = q;
        }
    }
}
