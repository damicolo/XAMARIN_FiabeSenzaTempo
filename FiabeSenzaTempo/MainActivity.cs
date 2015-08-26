using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System.Net;
using FiabeSenzaTempo;
using Android.Gms.Ads;


namespace FiabeSenzaTempo
{
	[Activity (Label = "Fiabe Senza Tempo", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		public class videoItem{
			public string Title;
			public string URL;
		}

		ListView myList;
		ArrayAdapter adapter;
		string theFileList = "";
		List<FiabeSenzaTempo.MainActivity.videoItem> theVideos = new List<FiabeSenzaTempo.MainActivity.videoItem>();


		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.Main);

			WebClient httpclient = new WebClient (); 
			theFileList = httpclient.DownloadString ("http://damicolo1.azurewebsites.net/FavoleSenzaTempoYoutube.txt");
			theFileList = theFileList.Replace ("\r\n", ",");
			string[] VideoList = theFileList.Split (new char[] { ','});
			List<string> myData = new List<string> ();
			foreach (string s in VideoList) {
				string[] elements = s.Split (new char[]{ ';' });
				theVideos.Add (new videoItem (){ Title = elements [1], URL = elements [2] });
				myData.Add (theVideos[theVideos.Count-1].Title);
			}
			myList = (ListView)this.FindViewById (Resource.Id.myListView);
			myList.ItemClick += myList_ItemClick;
			adapter = new ArrayAdapter (this, Android.Resource.Layout.SimpleListItem1,myData);
			myList.Adapter = adapter;

			AdView mAdView = (AdView) this.FindViewById(Resource.Id.adView);
			AdRequest adRequest = new AdRequest.Builder ().Build ();
			mAdView.LoadAd(adRequest);

		}
			
		private async void myList_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			videoItem sellectedItem = theVideos [e.Position];
			string videoID = sellectedItem.URL.Split (new char[] { '=' })[1];
			try
			{
				YouTubeUri theURI = await  YouTube.GetVideoUriAsync(videoID,YouTubeQuality.Quality720P);
				var uri = Android.Net.Uri.Parse(theURI.Uri.AbsoluteUri);
				var videoView = FindViewById<VideoView>(Resource.Id.videoView1);
				videoView.SetVideoURI(uri);
				videoView.Start ();
			}
			catch (Exception ex) 
			{
				Console.WriteLine (ex.ToString ());
			}
		}
	}
}


