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
	[Activity (Label = "Fiabe Senza Tempo", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
	public class MainActivity : Activity
	{
		private const string m_playlist = "http://damicolo1.azurewebsites.net/FavoleSenzaTempoYoutube.txt";
		ListView m_myList;
		ArrayAdapter m_adapter;
		List<videoItem> m_theVideos = new List<videoItem>();


		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);

			// prevent screen lock; require WAKE_LOCK permission in the manifest
			this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

			// retrieve the playlist from azure
			WebClient httpclient = new WebClient (); 
			string theFileList = httpclient.DownloadString (m_playlist);
			theFileList = theFileList.Replace ("\r\n", ",");
			string[] VideoList = theFileList.Split (new char[] { ','});
			List<string> myData = new List<string> ();
			foreach (string s in VideoList) {
				string[] elements = s.Split (new char[]{ ';' });
				m_theVideos.Add (new videoItem (){ Title = elements [1], URL = elements [2] });
				myData.Add (m_theVideos[m_theVideos.Count-1].Title);
			}

			// set the lis and adapter
			m_myList = (ListView)this.FindViewById (Resource.Id.myListView);
			m_myList.ItemClick += myList_ItemClick;
			m_adapter = new ArrayAdapter (this, Android.Resource.Layout.SimpleListItem1,myData);
			m_myList.Adapter = m_adapter;

			// advertising setup
			AdView mAdView = (AdView) this.FindViewById(Resource.Id.adView);
			AdRequest adRequest = new AdRequest.Builder ().Build ();
			mAdView.LoadAd(adRequest);
		}
			
		private async void myList_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			videoItem sellectedItem = m_theVideos [e.Position];
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


