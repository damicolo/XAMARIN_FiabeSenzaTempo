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
		private ListView m_myList;
		private FavoleListViewAdapter m_adapter;
		private VideoView videoView;
		private bool m_videoSourceSet = false;
		private List<videoItem> m_theVideos = new List<videoItem>();
		private System.Timers.Timer t;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);

			// prevent screen lock; require WAKE_LOCK permission in the manifest
			this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

			// retrieve the playlist from azure
			WebClient httpclient = new WebClient (); 
			string theFileList = "";
			try{
				theFileList = httpclient.DownloadString (m_playlist);
				theFileList = theFileList.Replace ("\r\n", ",");
			}
			catch(Exception ex) {
				Console.WriteLine (ex.ToString ());
			}
			string[] VideoList = theFileList.Split (new char[] { ','});
			List<string> myData = new List<string> ();
			foreach (string s in VideoList) {
				string[] elements = s.Split (new char[]{ ';' });
				if (elements.Length < 3)
					continue;
				m_theVideos.Add (new videoItem (){ Title = elements [1], URL = elements [2] });
				myData.Add (m_theVideos[m_theVideos.Count-1].Title);
			}

			// set the lis and adapter
			m_myList = (ListView)this.FindViewById (Resource.Id.myListView);
			//m_adapter = new ArrayAdapter (this, Android.Resource.Layout.SimpleListItem1,myData);
			m_adapter = new FavoleListViewAdapter (this, m_theVideos);
			m_myList.Adapter = m_adapter;
			m_myList.ItemClick += myList_ItemClick;

			t = new System.Timers.Timer ();
			t.Interval = 500;
			t.Elapsed += T_Elapsed;

			videoView = FindViewById<VideoView>(Resource.Id.videoView1);
			videoView.Touch += videoView_Touch;
			videoView.Info += VideoView_Info;
			videoView.Prepared += VideoView_Prepared;
			// advertising setup
			AdView mAdView = (AdView) this.FindViewById(Resource.Id.adView);
			AdRequest adRequest = new AdRequest.Builder ().Build ();
			mAdView.LoadAd(adRequest);
		}

		void T_Elapsed (object sender, System.Timers.ElapsedEventArgs e)
		{
			RunOnUiThread(() =>
				{
					Console.WriteLine(videoView.CurrentPosition.ToString());
				});
				
		}

		void VideoView_Info (object sender, Android.Media.MediaPlayer.InfoEventArgs e)
		{

		}

		void VideoView_Prepared (object sender, EventArgs e)
		{
			Console.WriteLine (videoView.Duration.ToString());
		}

		protected override void OnPause()
		{
			base.OnPause ();
		}

		protected override void OnResume()
		{
			base.OnResume ();
		}
			
		private void videoView_Touch(object sender, View.TouchEventArgs e)
		{
			if (e.Event.Action == MotionEventActions.Down) {
				if (videoView.IsPlaying)
					videoView.Pause ();
				else if (m_videoSourceSet)
					videoView.Start ();
			}	
		}
						
		private async void myList_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			videoItem sellectedItem = m_theVideos [e.Position];
			string videoID = sellectedItem.URL.Split (new char[] { '=' })[1];
			try
			{
				YouTubeUri theURI = await  YouTube.GetVideoUriAsync(videoID,YouTubeQuality.Quality720P);
				var uri = Android.Net.Uri.Parse(theURI.Uri.AbsoluteUri);
				videoView.SetVideoURI(uri);
				videoView.Start ();
				m_videoSourceSet = true;

				t.Enabled = true;
				t.Start();
			}
			catch (Exception ex) 
			{
				Console.WriteLine (ex.ToString ());
			}
		}
	}
}


