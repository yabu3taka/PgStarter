using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace PgStarter
{
    abstract class BaseViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyDataErrorInfo
        private readonly Dictionary<string, string> _errors = new();

        public System.Collections.IEnumerable GetErrors(string propertyName)
        {
            IEnumerable<string> f(string propertyName)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    return _errors.Values.ToList();
                }
                else if (_errors.ContainsKey(propertyName))
                {
                    return new string[] { _errors[propertyName] };
                }
                else
                {
                    return Enumerable.Empty<string>();
                }
            }
            return f(propertyName);
        }

        public bool HasErrors => _errors.Any();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void SetError(string propertyName, string newError)
        {
            string oldError = "";
            if (_errors.ContainsKey(propertyName))
            {
                oldError = _errors[propertyName];
            }
            if (oldError == newError)
            {
                return;
            }

            if (string.IsNullOrEmpty(newError))
            {
                _errors.Remove(propertyName);
            }
            else
            {
                _errors[propertyName] = newError;
            }
            RaiseErrorsChanged(propertyName);
        }

        protected void ClearError(params string[] propertyName)
        {
            Array.ForEach(propertyName, f =>
            {
                _errors.Remove(f);
                RaiseErrorsChanged(f);
            });
        }

        protected bool HasError(params string[] propertyName)
        {
            return propertyName.Any(f => _errors.ContainsKey(f));
        }

        protected ViewModelValidation StartValidation(string propertyName)
        {
            return new ViewModelValidation(this, propertyName);
        }

        public void EndValidation(ViewModelValidation trans)
        {
            SetError(trans.PropertyName, trans.NewError);
        }
        #endregion
    }

    class ViewModelValidation : IDisposable
    {
        private readonly BaseViewModel _baseModel;

        public ViewModelValidation(BaseViewModel baseModel, string propertyName)
        {
            _baseModel = baseModel;
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        public string NewError { get; private set; } = null;

        public bool NoError => string.IsNullOrEmpty(NewError);

        public void AddError(string error)
        {
            if (string.IsNullOrEmpty(NewError))
            {
                NewError = error;
            }
        }

        public void ValidateRuiredField(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                AddError("入力してください。");
                return;
            }
        }

        public void ValidateFileExist(string value, string[] exts)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!File.Exists(value))
            {
                AddError("存在するファイルを入力してください。");
                return;
            }

            if (exts != null)
            {
                foreach (string ext in exts)
                {
                    if (!value.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        AddError("正しいファイルを入力してください。");
                        return;
                    }
                }
            }
        }

        public void ValidateFolderExist(string value, params string[] others)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (Directory.Exists(value))
            {
                return;
            }
            foreach (string dir in others)
            {
                if (!string.IsNullOrEmpty(dir))
                {
                    if (Directory.Exists(dir))
                    {
                        return;
                    }
                }
            }

            AddError("存在するフォルダを入力してください。");
        }

        public void Dispose()
        {
            _baseModel.EndValidation(this);
        }
    }

    abstract class BaseCommand : ICommand
    {
        private readonly HashSet<string> _propertyNames = new();

        protected void AddProertyName(INotifyPropertyChanged notifyer, params string[] propertyNames)
        {
            foreach (string n in propertyNames)
            {
                _propertyNames.Add(n);
            }
            notifyer.PropertyChanged -= PropertyChangedHandler;
            notifyer.PropertyChanged += PropertyChangedHandler;
        }

        private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (_propertyNames.Contains(e.PropertyName))
            {
                CanExecuteChanged?.Invoke(sender, EventArgs.Empty);
            }
        }

        public abstract bool CanExecute(object parameter);

        public event EventHandler CanExecuteChanged;

        public abstract void Execute(object parameter);
    }

    class DelegateCommand : BaseCommand
    {
        private readonly Action _cmdAction;
        private Func<bool> _cmdEnable = null;

        public DelegateCommand(Action a)
        {
            _cmdAction = a;
        }

        public DelegateCommand SetCanExecute(INotifyPropertyChanged notifyer,
            Func<bool> f,
            params string[] propertyNames)
        {
            _cmdEnable = f;
            AddProertyName(notifyer, propertyNames);
            return this;
        }

        public override bool CanExecute(object parameter)
        {
            if (_cmdEnable == null)
            {
                return true;
            }
            return _cmdEnable();
        }

        public override void Execute(object parameter)
        {
            _cmdAction();
        }
    }
}
