using System;
using System.Collections.Generic;

namespace Code.Game.UI.FastReactive
{
    public class ReactiveProperty<T>
    {
        private T value;

        public T Value
        {
            get => value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(this.value, value)) return;
                this.value = value;
                OnValueChanged?.Invoke(this.value);
            }
        }

        public event Action<T> OnValueChanged;
        
        public void Bind(Action<T> action)
        {
            OnValueChanged = action;
            
            action?.Invoke(value);
        }
    }
}