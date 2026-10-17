using CommunityToolkit.Mvvm.ComponentModel;

namespace ServerPickerX.Models
{
    public partial class PresetServerModel : ObservableObject
    {
        public ServerModel ServerModel { get; }

        public string Key { get; }

        public string Flag => ServerModel.Flag;

        // One image per country, so the path alone groups servers by flag
        public string FlagSortKey => ServerModel.Flag;

        public string Name => ServerModel.Name;

        public string Description => ServerModel.Description;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAllowed))]
        private bool isBlocked;

        // The preset editor asks which servers you want to play on, the stored
        // preset still lists the blocked ones
        public bool IsAllowed
        {
            get => !IsBlocked;
            set => IsBlocked = !value;
        }

        public PresetServerModel(ServerModel serverModel, string key, bool isBlocked)
        {
            ServerModel = serverModel;
            Key = key;
            IsBlocked = isBlocked;
        }
    }
}
