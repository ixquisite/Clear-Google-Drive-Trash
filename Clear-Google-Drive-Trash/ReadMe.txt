********************************************************************************
* ReadMe.txt                                                                   *
********************************************************************************


2014-10-26 - First cut of Clear-Google-Drive-Trash

Release notes:
Goal is to write a MS Windows console app in MS VS C# to prove concept an 
implementation of the Google Drive API.
Note(!) that we are not using the Google Drive SDK here as that integration 
would not provide workflow support for devices (aka:head less devices).
The idea is to use Google Drive API specifically supporting Devices. This 
implementation could be utilized by any Windows Service not offering UI 
support. Google Drive SDK requires UI support for user authentication process.
More details here: https://accounts.google.com/o/oauth2/device/code

Pre-Requisites:
You must create an account within Google's Developer Console:
https://console.developers.google.com
You will need to enable the Drive API.

Once you have done that, you will need to update the <appSettings> sections of
App.config. 

If left as is, when executing, you will get the following error response from 
Google:
invalid_scope:Not authorized to request the scopes:
[https://www.googleapis.com/auth/drive]

Optionally, you can comment out this line 
LN37: scope += "https://www.googleapis.com/auth/drive";
and you will see that you will get a valid response. However, this 
app (your Google apps developer account) won't get added to the user's list of
authorized applications within Google Drive.

Since the response from Google keeps being the same:
authorization_pending
I wonder if that API is fully functional or if there is something missing in my
account setup, which won't add my app to the user's list of authenticated apps.


Question here is if Google Drive API has a bug, or if they left out the fact 
that Google Drive API cannot get full access to a user's Drive account.
