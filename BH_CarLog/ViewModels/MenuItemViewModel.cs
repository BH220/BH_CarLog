using BH_CarLog.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.ViewModels
{
    /// <summary>좌측 메뉴 항목 (기존 MenuBtn)</summary>
    public partial class MenuItemViewModel : ObservableObject
    {
        public Menus MenuId { get; }
        public string Title { get; }

        /// <summary>Ctrl + Shift + 단축키</summary>
        public string Shortcut { get; }
        public string ShortcutText => $"Ctrl+Shift+{Shortcut}";
        public string Icon { get; }

        [ObservableProperty]
        private bool _isSelected;

        public MenuItemViewModel(Menus menuId, string title, string shortcut, string iconFile)
        {
            MenuId = menuId;
            Title = title;
            Shortcut = shortcut;
            Icon = $"pack://application:,,,/Assets/Images/{iconFile}";
        }
    }
}
