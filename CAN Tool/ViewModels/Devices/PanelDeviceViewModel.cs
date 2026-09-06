namespace OmniProtocol
{
    // Пульт управления (id.Type == 126, devType "Panel" в omnidata.json) - это и есть ПУ28,
    // работающий в обычном режиме (не в загрузчике), поэтому наследуется от Pu28DeviceViewModel:
    // те же команды управления внешней flash-микросхемой (стирание, дамп, слоты - PGN107-109),
    // что и на странице загрузчика (BootloaderDeviceViewModel), только под своим собственным
    // адресом (Id.Type==126 вместо 123). Плюс версии сохранённого ПО (PGN110/14-17), которые
    // пульт сам периодически транслирует - см. Pu28DeviceViewModel.OwnImageVersion и т.д.
    public partial class PanelDeviceViewModel : Pu28DeviceViewModel
    {
        public PanelDeviceViewModel(DeviceId id) : base(id) { }
    }
}
