Hệ thống UI
- UIManager: Quản lý View và Popup
- ViewManager: Quản lý View
- PopupManager: Quản lý Popup
- View: View là màn hình chính của game
- Popup: Popup là màn hình phụ của game
- UIAnimation: Animation của View và Popup

Cấu trúc thư mục
- Core: Core của hệ thống UI
    - UIManager.cs: Quản lý View và Popup
    - ViewManager.cs: Quản lý View
    - PopupManager.cs: Quản lý Popup
    - View.cs: View là màn hình chính của game
    - Popup.cs: Popup là màn hình phụ của game
    - UIAnimation.cs: Animation của View và Popup
Game hiện tại:
_Master/UI
    - Background: Background của game
    - TopBar: TopBar của game
    - MainMenu: MainMenu của game => chứa các thông chung của game (đại khái là màn hình hôm)
    - MapSelection: MapSelection của game => Màn hình chọn map
    - LoadingScreen: LoadingScreen của game => Màn hình loading
- Trong mỗi game:
    - GameName (Ví dụ: TranHuongDao): UIs của game map. Mỗi map sẽ có hệ thống UI khác nhau. Dưới đây là ví dụ về UI của game TranHuongDao: 
        - GameHUD: GameHUD của game
        - ShopPopup: ShopPopup của game
        - MarketPopup: MarketPopup của game
        - FarmPopup: FarmPopup của game
        - GuidePopup: GuidePopup của game

Flow từ home đến UI của map:
- Home => MapSelection => LoadingScreen => GameHUD 
Mỗi game sẽ có scene riêng, trong đó có hệ thống UI riêng. 

