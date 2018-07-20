﻿using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using FuelApp.Modal;
using FuelUED.CommonFunctions;
using FuelUED.Modal;
using FuelUED.Service;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace FuelUED
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = false)]
    public class MainScreenActivity : AppCompatActivity
    {
        private LinearLayout mainLayout;
        private ProgressBar loader;
        private RelativeLayout mainHolder;
        private Button btnDownloadData;

        public string ipadress { get; private set; }
        public bool IsDeviceAvailable { get; private set; }

        private string siteId;
        private string deviceId;
        private Button btnUploadData;
        private TableQuery<FuelEntryDetails> fuelDetails;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MainScreen);

            mainLayout = FindViewById<LinearLayout>(Resource.Id.layHolder);
            loader = FindViewById<ProgressBar>(Resource.Id.loader);
            mainHolder = FindViewById<RelativeLayout>(Resource.Id.mainRelativeHolder);

            //var pref = PreferenceManager.GetDefaultSharedPreferences(this);
            //Ipadress = pref.GetString(Utilities.IPAddress, string.Empty);

            //Get IPAdress for preference
            ipadress = AppPreferences.GetString(this, Utilities.IPAddress);
            siteId = AppPreferences.GetString(this, Utilities.SITEID);
            deviceId = AppPreferences.GetString(this, Utilities.DEVICEID);

            WebService.IPADDRESS = ipadress;

            FindViewById<Button>(Resource.Id.btnFuelEntry).Click += (s, e) =>
             {
                 //VehicleList = FuelDB.Singleton.GetValue().ToList();

                 if (FuelDB.Singleton.DBExists() && FuelDB.Singleton.GetBillDetails() != null)
                 {
                     if (AppPreferences.GetString(this, Utilities.DEVICESTATUS).Equals("1"))
                     {
                         StartActivity(typeof(FuelActivity));
                     }
                     else
                     {
                         Toast.MakeText(this, "Device not avalable", ToastLength.Short).Show();
                     }
                 }
                 else
                 {
                     var alertDialog1 = new Android.App.AlertDialog.Builder(this);
                     alertDialog1.SetTitle("you need to sync first");
                     alertDialog1.SetPositiveButton("OK", (ss, se) => { });
                     alertDialog1.Show();
                 }
                 // SyncButton_Click();
             };

            btnDownloadData = FindViewById<Button>(Resource.Id.btnDownloadData);
            btnDownloadData.Click += (s, e) =>
            {
                SyncButton_Click();
            };

            btnUploadData = FindViewById<Button>(Resource.Id.btnUploadData);
            btnUploadData.Click += (s, e) =>
            {
                UploadDataToServer();
            };
        }

        private void UploadDataToServer()
        {
            try
            {
                fuelDetails = FuelDB.Singleton.GetFuelValues();
            }
            catch { }
            if (fuelDetails == null)
            {
                return;
            }
            var billDetails = FuelDB.Singleton.GetBillDetails()?.First();
            var list = new List<UploadDetails>();
            foreach (var item in fuelDetails)
            {
                list.Add(new UploadDetails
                {
                    CID = billDetails.BillCurrentNumber == string.Empty ? "0" : billDetails.BillCurrentNumber,
                    DID = AppPreferences.GetString(this, Utilities.DEVICEID) == string.Empty ? "0" : AppPreferences.GetString(this, Utilities.DEVICEID),
                    SID = AppPreferences.GetString(this, Utilities.SITEID) == string.Empty ? "0" : AppPreferences.GetString(this, Utilities.SITEID),
                    CStock = billDetails.AvailableLiters == string.Empty ? "0" : billDetails.AvailableLiters,
                    ClosingKM = item.ClosingKMS == string.Empty ? "0" : item.ClosingKMS,
                    DriverID = item.DriverID_PK == string.Empty ? "0" : item.DriverID_PK,
                    DriverName = item.DriverName == string.Empty ? "0" : item.DriverName,
                    FilledBy = item.FilledBy == string.Empty ? "0" : item.FilledBy,
                    FuelDate = item.CurrentDate == string.Empty ? "0" : item.CurrentDate,
                    FuelLts = item.FuelInLtrs == string.Empty ? "0" : item.FuelInLtrs,
                    FuelNo = item.BillNumber == string.Empty ? "0" : item.BillNumber,
                    FuelSource = item.FuelStockType == string.Empty ? "0" : item.FuelStockType,
                    KMPL = item.Kmpl == string.Empty ? "0" : item.Kmpl,
                    OpeningKM = item.OpeningKMS == string.Empty ? "0" : item.OpeningKMS,
                    RegNo = item.VehicleNumber == string.Empty ? "0" : item.VehicleNumber,
                    VType = item.VehicleType == string.Empty ? "0" : item.VehicleType,
                    Rate = item.RatePerLtr == string.Empty ? "0" : item.RatePerLtr,
                    TAmount = item.Price == string.Empty ? "0" : item.Price,
                    Remarks = item.Remarks == string.Empty ? "0" : item.Remarks,
                    TransType = item.FuelType == string.Empty ? "0" : item.FuelType,
                    Mode = item.PaymentType == string.Empty ? "0" : item.PaymentType,
                    VehicleID = item.VID == string.Empty ? "0" : item.VID,
                    MeterFault = item.MeterFault == string.Empty ? "0" : item.MeterFault,
                    TotalKM = item.TotalKM == string.Empty ? "0" : item.Kmpl
                });
            }
            Console.WriteLine(list);
            var deserializedData = JsonConvert.SerializeObject(list);
            Console.WriteLine(deserializedData);
            var resposeAfterPost = WebService.PostAllDataToWebService("UPFStock", deserializedData);
            try
            {
                var vehicleList = JsonConvert.DeserializeObject<List<VehicleDetails>>(resposeAfterPost);
                CreateDatabaseOrModifyDatabase(vehicleList);
            }
            catch { }
            Console.WriteLine(resposeAfterPost);
        }

        private void SyncButton_Click()
        {
            if (ipadress.Equals(string.Empty) || siteId.Equals(string.Empty) || deviceId.Equals(string.Empty))
            {
                Toast.MakeText(this, "Something went wrong..", ToastLength.Short).Show();
                //StartActivity(typeof(ConfigActivity));
                return;
            }
            RunOnUiThread(() =>
            {
                //    //Toast.MakeText(this, "Please wait..", ToastLength.Short).Show();
                mainHolder.Alpha = 0.5f;
                loader.Visibility = Android.Views.ViewStates.Visible;
                Window.SetFlags(Android.Views.WindowManagerFlags.NotTouchable, Android.Views.WindowManagerFlags.NotTouchable);
            });

            var thread = new Thread(new ThreadStart(delegate
            {
                var resposeString = WebService.PostDeviceAndSiteIDToWebService("GetVD", deviceId, siteId);
                //var fuelStockRes = WebService.GetDataFromWebService("LoadFStock");
                try
                {
                    var VehicleList = JsonConvert.DeserializeObject<List<VehicleDetails>>(resposeString);
                    //var fuelStoc = JsonConvert.DeserializeObject<List<Fuel>>(fuelStockRes);
                    //Console.WriteLine(fuelStoc);
                    CreateDatabaseOrModifyDatabase(VehicleList);

                }
                catch (Exception ec)
                {
                    Console.WriteLine(ec.Message);
                    RunOnUiThread(() =>
                    {
                        Toast.MakeText(this, "Something wrong ...Check connectivity..", ToastLength.Short).Show();
                    });
                }
                RunOnUiThread(() =>
                {
                    loader.Visibility = Android.Views.ViewStates.Gone;
                    //pd.Hide();
                    //syncButton.Clickable = true;
                    mainHolder.Alpha = 1f;
                    Window.ClearFlags(Android.Views.WindowManagerFlags.NotTouchable);
                });
                //Toast.MakeText(this, "Stored in Database", ToastLength.Short).Show();
            }));
            thread.Start();
            // loader.Visibility = Android.Views.ViewStates.Visible;
            // loader.Visibility = Android.Views.ViewStates.Gone;
        }

        private void CreateDatabaseOrModifyDatabase(List<VehicleDetails> vehicleList)
        {
            DeleteDatabase(FuelDB.Singleton.DBPath);
            FuelDB.Singleton.CreateTable<VehicleDetails>();
            FuelDB.Singleton.CreateTable<BillDetails>();

            //FuelDB.Singleton.CreateDatabase<Fuel>();

            var details = vehicleList?.First();
            vehicleList.RemoveAt(0);
            var billDetails = new BillDetails
            {
                AvailableLiters = details.VID,
                BillCurrentNumber = details.DriverID_PK,
                BillPrefix = details.RegNo,
                DeviceStatus = details.DriverName
            };

            AppPreferences.SaveString(this, Utilities.DEVICESTATUS, billDetails.DeviceStatus);
            FuelDB.Singleton.InsertValues(vehicleList);
            FuelDB.Singleton.InsertBillDetails(billDetails);
        }
    }
}