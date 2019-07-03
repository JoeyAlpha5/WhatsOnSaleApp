using Android.App;
using Android.OS;
/*using Android.Support.V7.App;*/
using Android.Runtime;
using Android.Widget;
using Android.Webkit;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Support.V4.App;
using Android;
using Android.Content.PM;
using System;
using Android.Util;
using Plugin.Geolocator;
using Android.Provider;
using System.Collections.Generic;
using Android.Locations;

namespace WhatsOnSale
{
    [Activity(Label = "WhatsOnSale", Icon = "@mipmap/ic_icon", RoundIcon = "@mipmap/ic_launcher_round", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : Activity
    {
        static WebView MyWebvIew;
        static ProgressBar MyProgressBar;
        static TextView whatsonsaleText;
        public static MainActivity Instance;
        static string FCMtoken;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            MyWebvIew = FindViewById<WebView>(Resource.Id.webView1);
            MyProgressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            MyWebvIew.Settings.UserAgentString = "WhatsOnSaleApp";
            MyWebvIew.SetWebViewClient(new Client());
            MyWebvIew.SetWebChromeClient(new MyWebChromeClient());
            MyWebvIew.Settings.JavaScriptEnabled = true;
            MyWebvIew.Settings.DomStorageEnabled = true;
            MyWebvIew.Settings.SetAppCacheEnabled(true);
            MyWebvIew.Settings.SetGeolocationEnabled(true);
            Instance = this;
            FCMtoken = "";
            //text
            whatsonsaleText = FindViewById<TextView>(Resource.Id.text);
            Typeface typeface = Typeface.CreateFromAsset(Assets, "BAUHS93.TTF");
            whatsonsaleText.SetTypeface(typeface, TypefaceStyle.Normal);


            //set the permissions here
            String[] Permissions = new String[3] { Manifest.Permission.AccessFineLocation, Manifest.Permission.ReadContacts, Manifest.Permission.AccessCoarseLocation };
            //Log.Info("hello world", ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadContacts).ToString());
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != (int)Permission.Granted && Build.VERSION.SdkInt >= BuildVersionCodes.M || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadContacts) != (int)Permission.Granted && Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                int REQUEST_PERMISION = 0;
                ActivityCompat.RequestPermissions(this, Permissions, REQUEST_PERMISION);
                MyWebvIew.LoadUrl("https://whatsonsale.herokuapp.com/auth/auth");
            }
            else
            {
                MyWebvIew.LoadUrl("https://whatsonsale.herokuapp.com/auth/auth");
            }

          

        }

        //save the user's token
        public static void saveUserToken(string token)
        {
            FCMtoken = token;
        }


        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            //the data uri

            if (data != null) {

                var result = data.Data;

                //what you want from the data, in this case the mobile number
                string[] projection = { ContactsContract.CommonDataKinds.Phone.Number };
                var cursor = ContentResolver.Query(result, projection, null, null, null);
                //do what has to be done after selcting the contact
                if (cursor.MoveToFirst())
                {


                    //get the first number,since it's one contact selected, it'll be one number
                    var mobileNumber = cursor.GetString(cursor.GetColumnIndex(ContactsContract.CommonDataKinds.Phone.Number));
                    //javascript method to call once confirmation has been received
                    var script = string.Format("javascript:SearchUsingMobileContact('{0}');", mobileNumber);

                    //intent to confirm share 
                    AlertDialog.Builder Alert = new AlertDialog.Builder(this);
                    Alert.SetTitle("Share basket");
                    Alert.SetMessage("Continue to share your basket with:" + mobileNumber + "");
                    Alert.SetPositiveButton("Yes", delegate { MyWebvIew.EvaluateJavascript(script, null); });
                    Alert.SetNegativeButton("Cancel", delegate { Alert.Dispose(); });
                    Alert.Show();

                }


            }

        }


         //check if location is turned on
        public bool loactionStatus()
        {
            LocationManager locManager = (LocationManager)GetSystemService(LocationService);
            bool enabled = locManager.IsProviderEnabled(LocationManager.GpsProvider);

            if (enabled == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //get contacts 
        public static void GetContact()
        {

            String[] Permissions = new String[1] { Manifest.Permission.ReadContacts};
            if (ActivityCompat.CheckSelfPermission(Instance, Manifest.Permission.ReadContacts) != (int)Permission.Granted && Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                int REQUEST_PERMISION = 0;
                ActivityCompat.RequestPermissions(Instance, Permissions, REQUEST_PERMISION);
            }
            else
            {

                //string[] projection = { ContactsContract.Contacts.InterfaceConsts.Id, ContactsContract.CommonDataKinds.Phone.Number };
                var uri = ContactsContract.CommonDataKinds.Phone.ContentUri;
                Intent pickcontact = new Intent(Intent.ActionPick, uri);
                //Application.Context.StartActivity(pickcontact);
                Instance.StartActivityForResult(pickcontact, 0);
            }

        }


        //location
        public async void LoadUserCordinates()
        {
            //location is on
            bool location_on = loactionStatus();

            Log.Info("Location is on", location_on.ToString());

            if (location_on == false)
            {
                var toast = Toast.MakeText(this, "Please enable location to access this feature", ToastLength.Long);
                toast.Show();
            }
            else
            {
                String[] Permissions = new String[2] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation };
                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != (int)Permission.Granted && Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    int REQUEST_PERMISION = 0;
                    ActivityCompat.RequestPermissions(this, Permissions, REQUEST_PERMISION);
                }
                else
                {
                    var locater = CrossGeolocator.Current;
                    locater.DesiredAccuracy = 50;
                    var position = await locater.GetPositionAsync(TimeSpan.FromMilliseconds(100));
                    string latitude = position.Latitude.ToString();
                    string longitude = position.Longitude.ToString();
                    var script = string.Format("userLocation('{0}','{1}');", latitude, longitude);
                    MyWebvIew.EvaluateJavascript(script, null);
                }
            }


        }



        //geolocation
        public class MyWebChromeClient: WebChromeClient
        {
            public override void OnGeolocationPermissionsShowPrompt(string origin, GeolocationPermissions.ICallback callback)
            {
                callback.Invoke(origin, true, false);
            }
        }

        //webview client
        public class Client: WebViewClient
        {

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {

                if (url.Contains("https://whatsonsale.herokuapp.com/auth/auth") || url.Contains("https://whatsonsale.herokuapp.com/login/login") || url.Contains("https://whatsonsale.herokuapp.com/logout/logout"))
                {
                   
                    return false;
                }
                else
                {
                    var uri = Android.Net.Uri.Parse(url);
                    var intent = new Intent(Intent.ActionView, uri);
                    intent.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intent);
                    return true;
                }

            }


            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                //change tab color
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                {
                    if (url.Contains("https://whatsonsale.herokuapp.com/login/login") || url.Contains("https://whatsonsale.herokuapp.com/logout/logout"))
                    {

                        Instance.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                        Instance.Window.SetStatusBarColor(Color.Rgb( 218, 14, 47));
                    }       
                    else
                    {
                        Instance.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                        Instance.Window.SetStatusBarColor(Color.Rgb(175, 172, 172));
                    }
                }

                MyProgressBar.Visibility = ViewStates.Visible;
                whatsonsaleText.Visibility = ViewStates.Invisible;

            }



            public override void OnPageFinished(WebView view, string url)
            {
                MyProgressBar.Visibility = ViewStates.Invisible;


                //view coordinates on map when locate ic
                if (url.Contains("https://whatsonsale.herokuapp.com/auth/auth/#top"))
                {
                    Instance.LoadUserCordinates();
                }

                if (url.Contains("https://whatsonsale.herokuapp.com/auth/auth/#selectMobileContact"))
                {
                    GetContact();
                }

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                {
                    //change the tab color is user is on news 
                    if (url.Contains("https://whatsonsale.herokuapp.com/auth/auth/#news"))
                    {
                        Instance.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                        Instance.Window.SetStatusBarColor(Color.Rgb(8, 62, 105));
                    }
                    else if (url.Contains("https://whatsonsale.herokuapp.com/login/login") || url.Contains("https://whatsonsale.herokuapp.com/logout/logout"))
                    {

                        Instance.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                        Instance.Window.SetStatusBarColor(Color.Rgb(218, 14, 47));
                    }
                    else
                    {
                        Instance.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                        Instance.Window.SetStatusBarColor(Color.Rgb(175, 172, 172));
                    }
                }


                //call javascript function to save user token
                if (FCMtoken != "")
                {
                    var script = string.Format("saveUserToken('{0}')", FCMtoken);
                    MyWebvIew.EvaluateJavascript(script, null);
                }



            }



            public override void OnReceivedError(WebView view, [GeneratedEnum] ClientError errorCode, string description, string failingUrl)
            {
                view.LoadUrl("file:///android_asset/index.html");
            }

            

        }



        public override void OnBackPressed()
        {
            var script = string.Format("back()");
            MyWebvIew.EvaluateJavascript(script, null);
        }

      
    }
}