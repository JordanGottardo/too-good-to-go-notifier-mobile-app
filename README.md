**TgTgNotifier** is a mobile Android application which sends a notification when your favourite products are available on [TooGoodToGo](https://toogoodtogo.com/en-us). 

This application is client of [**TooGoodToGo Cloud Notifier**](https://github.com/JordanGottardo/too-good-to-go-cloud-notifier) server component and requires it in order to function correctly.

This app has been developed using [Xamarin.Android](https://docs.microsoft.com/en-us/xamarin/android/).

# Installation
You can install [TgTgNotifier directly from the Google Play Store](https://play.google.com/store/apps/details?id=com.jordangottardo.tgtgnotifier) (available in Italian and English).


# Configuration
## Notification and battery savings options
Before proceeding with configuration, it is important to notice that this app uses a periodic background worker in order to retrieve products availability information and show notifications when those products are available.

Many Android vendors employ battery savings and optimizations on their devices in order to prolong battery life. Those optimizations could break this application. Hence, if you don't receive any notification from TgTgNotifier, please disable battery savings options for this app on your device. More info on how [dontkillmyapp.com](https://dontkillmyapp.com/).
## Product monitoring
