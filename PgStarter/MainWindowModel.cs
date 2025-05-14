using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;

using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace PgStarter
{
    class MainWindowModel : BaseViewModel
    {
        public MainWindowModel()
        {
            _loader = new ProgramInfoLoader(FileUtil.GetAssemblyFolder());
            Programs = new ObservableCollection<ProgramInfo>(_loader.Load());

            InitCommand();
        }

        #region Programs
        private readonly ProgramInfoLoader _loader;

        public ObservableCollection<ProgramInfo> Programs { get; }
        #endregion

        #region Selection
        private bool _notRunning = true;
        public bool NotRunning
        {
            get => _notRunning;
            set
            {
                if (value == _notRunning)
                {
                    return;
                }
                _notRunning = value;

                RaisePropertyChanged();
            }
        }

        private ProgramInfo _targetProgram;
        public ProgramInfo TargetProgram
        {
            get => _targetProgram;
            set
            {
                if (value == _targetProgram)
                {
                    return;
                }
                _targetProgram = value;

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TargetProgramSelected));
            }
        }

        public bool TargetProgramSelected => _targetProgram != null;

        private ProgramInfo _editProgram;
        public ProgramInfo EditProgram
        {
            get => _editProgram;
            set
            {
                if (value == _editProgram)
                {
                    return;
                }
                _editProgram = value;

                ProgramName = _editProgram?.ProgramName ?? "";
                ProgramPath = _editProgram?.ProgramPath ?? "";
                MyDocPath = _editProgram?.MyDocPath ?? "";

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(EditProgramSelected));

                ClearError(nameof(ProgramName), nameof(ProgramPath), nameof(MyDocPath));
            }
        }

        public bool EditProgramSelected => _editProgram != null;
        #endregion

        #region Program Info 
        private string _programName = "";
        public string ProgramName
        {
            get => _programName;
            set
            {
                if (value == _programName)
                {
                    return;
                }
                _programName = value;


                RaisePropertyChanged();
            }
        }

        private bool ValidateProgramName()
        {
            using (var c = StartValidation(nameof(ProgramName)))
            {
                string value = ProgramName;
                c.ValidateRuiredField(value);

                return c.NoError;
            }
        }

        private string _programPath = "";
        public string ProgramPath
        {
            get => _programPath;
            set
            {
                if (value == _programPath)
                {
                    return;
                }
                _programPath = value;

                RaisePropertyChanged();
            }
        }

        private bool ValidateProgramPath()
        {
            using (var c = StartValidation(nameof(ProgramPath)))
            {
                string value = ProgramPath;
                c.ValidateRuiredField(value);
                c.ValidateFileExist(value, new string[] { ".exe" });

                return c.NoError;
            }
        }

        private string _myDocPath = "";
        public string MyDocPath
        {
            get => _myDocPath;
            set
            {
                if (value == _myDocPath)
                {
                    return;
                }
                _myDocPath = value;

                RaisePropertyChanged();
            }
        }

        private bool ValidateMyDocPath()
        {
            using (var c = StartValidation(nameof(MyDocPath)))
            {
                string value = MyDocPath;
                c.ValidateRuiredField(value);

                string swapedDir = "";
                try
                {
                    swapedDir = FileUtil.GetMyFolder(value);
                }
                catch (Exception)
                {
                }
                c.ValidateFolderExist(value, swapedDir);

                return c.NoError;
            }
        }
        #endregion

        #region Command
        private void InitCommand()
        {
            AddCommand = new DelegateCommand(AddProgram);
            DelCommand = new DelegateCommand(DelProgram)
                .SetCanExecute(this, () => EditProgramSelected, nameof(EditProgramSelected));

            SelectCommand = new DelegateCommand(SelectProgram)
                .SetCanExecute(this, () => EditProgramSelected, nameof(EditProgramSelected));
            TestRunCommand = new DelegateCommand(TestRunProgram)
                .SetCanExecute(this, () => EditProgramSelected, nameof(EditProgramSelected));
            SelectFolderCommand = new DelegateCommand(SelectProgramFolder)
                .SetCanExecute(this, () => EditProgramSelected, nameof(EditProgramSelected));

            SaveCommand = new DelegateCommand(SaveProgram)
                .SetCanExecute(this, () => EditProgramSelected, nameof(EditProgramSelected));
            ResetCommand = new DelegateCommand(ResetProgram)
                .SetCanExecute(this, () => EditProgramSelected, nameof(EditProgramSelected));

            RunCommand = new DelegateCommand(RunProgram)
                .SetCanExecute(this, () => TargetProgramSelected && NotRunning,
                nameof(TargetProgramSelected), nameof(NotRunning));
        }

        public DelegateCommand AddCommand { get; private set; }

        private void AddProgram()
        {
            var info = new ProgramInfo(Programs.Count + 1);
            Programs.Add(info);
            EditProgram = info;

            _loader.Save(Programs);
        }

        public DelegateCommand DelCommand { get; private set; }

        private void DelProgram()
        {
            if(MessageBox.Show("削除しますか", "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            {
                return;
            }

            Programs.Remove(EditProgram);
            _loader.Save(Programs);
        }

        public DelegateCommand SelectCommand { get; private set; }

        private void SelectProgram()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "実行ファイル (*.exe)|*.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                ProgramPath = dialog.FileName;
            }
        }

        public DelegateCommand TestRunCommand { get; private set; }

        private void TestRunProgram()
        {
            bool ok = ValidateProgramPath();
            if (!ok)
            {
                return;
            }

            using Process _ = Start(ProgramPath);
        }

        public DelegateCommand SelectFolderCommand { get; private set; }

        private void SelectProgramFolder()
        {
            var cofd = new Forms.FolderBrowserDialog()
            {
                Description = "フォルダを選択",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            };

            if (cofd.ShowDialog() == Forms.DialogResult.OK)
            {
                MyDocPath = cofd.SelectedPath;
            }
        }

        public DelegateCommand SaveCommand { get; private set; }

        private void SaveProgram()
        {
            bool ok = ValidateProgramName() &
                ValidateProgramPath() &
                ValidateMyDocPath();
            if (!ok)
            {
                return;
            }

            EditProgram.ProgramName = ProgramName;
            EditProgram.ProgramPath = ProgramPath;
            EditProgram.MyDocPath = MyDocPath;

            RaisePropertyChanged(nameof(Programs));

            _loader.Save(Programs);

            MessageBox.Show("保存しました");
        }

        public DelegateCommand ResetCommand { get; private set; }

        private void ResetProgram()
        {
            ProgramName = EditProgram.ProgramName;
            ProgramPath = EditProgram.ProgramPath;
            MyDocPath = EditProgram.MyDocPath;

            MessageBox.Show("リセットしました");
        }

        public DelegateCommand RunCommand { get; private set; }

        private void RunProgram()
        {
            string origDir = TargetProgram.MyDocPath;
            string swapedDir = FileUtil.GetMyFolder(origDir);

            if (Directory.Exists(swapedDir))
            {
                FileUtil.MoveSafe(swapedDir, origDir);
            }

            NotRunning = false;

            var process = Start(TargetProgram.ProgramPath);
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) =>
            {
                FileUtil.MoveSafe(origDir, swapedDir);
                Application.Current.Dispatcher.Invoke(() => NotRunning = true);
            };
        }

        private static Process Start(string prog)
        {
            var startInfo = new ProcessStartInfo(prog);
            return Process.Start(startInfo);
        }
        #endregion
    }
}
