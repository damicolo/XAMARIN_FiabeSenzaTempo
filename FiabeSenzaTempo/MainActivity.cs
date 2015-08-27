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
using System.Threading.Tasks;
using Android.Graphics;
using System.Net.Http;


namespace FiabeSenzaTempo
{
	[Activity (Label = "Fiabe Senza Tempo", MainLauncher = true, Icon = "@drawable/favolesenzatempo", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
	public class MainActivity : Activity
	{
		private const string m_playlist = "http://damicolo1.azurewebsites.net/FavoleSenzaTempoYoutube.txt";
		private ListView m_myList;
		private FavoleListViewAdapter m_adapter;
		private VideoView videoView;
		private bool m_videoSourceSet = false;
		private List<videoItem> m_theVideos = new List<videoItem>();
		private System.Timers.Timer m_videoPregressTimer;

		protected override async void OnCreate (Bundle bundle)
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

				videoItem tempItem = new videoItem (){ Title = elements [1], URL = elements [2], ImageURL = elements [3] };
				//tempItem.Image = await GetImageFromUrl(tempItem.ImageURL);
				m_theVideos.Add (tempItem);
				myData.Add (m_theVideos[m_theVideos.Count-1].Title);
			}

			// parellel download of all the miniatures
			var downloadTasksQuery = new Task<Bitmap>[m_theVideos.Count];
			for (int i = 0; i < m_theVideos.Count; i++) {
				downloadTasksQuery [i] = GetImageFromUrl (m_theVideos [i].ImageURL);
			}				
			Bitmap[] myImages = await Task.WhenAll (downloadTasksQuery);
			for (int i = 0; i < m_theVideos.Count; i++) {
				m_theVideos [i].Image = myImages [i];
			}
				
			// set the lis and adapter
			m_myList = (ListView)this.FindViewById (Resource.Id.myListView);
			m_adapter = new FavoleListViewAdapter (this, m_theVideos);
			m_myList.Adapter = m_adapter;

			m_myList.ItemClick += M_myList_ItemClick;
			//m_myList.ItemLongClick += M_myList_ItemLongClick;

			m_videoPregressTimer = new System.Timers.Timer ();
			m_videoPregressTimer.Interval = 500;
			m_videoPregressTimer.Elapsed += T_Elapsed;

			videoView = FindViewById<VideoView>(Resource.Id.videoView1);
			videoView.Touch += videoView_Touch;
			videoView.Prepared += VideoView_Prepared;
			// advertising setup
			AdView mAdView = (AdView) this.FindViewById(Resource.Id.adView);
			AdRequest adRequest = new AdRequest.Builder ().Build ();
			mAdView.LoadAd(adRequest);
		}

		private async Task<Bitmap> GetImageFromUrl(string url)
		{
			using(var client = new HttpClient())
			{
				var msg = await client.GetAsync(url);
				if (msg.IsSuccessStatusCode)
				{
					using(var stream = await msg.Content.ReadAsStreamAsync())
					{
						﻿var bitmap = await BitmapFactory.DecodeStreamAsync(stream);
						return bitmap;
					}
				}
			}
			return null;
		}

		async void M_myList_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			((LinearLayout.LayoutParams)videoView.LayoutParameters).Weight = 40f;
			Console.WriteLine ("M_myList_ItemClick " + e.Position.ToString ());
			videoItem sellectedItem = m_theVideos [e.Position];
			string videoID = sellectedItem.URL.Split (new char[] { '=' })[1];
			try
			{
				YouTubeUri theURI = await  YouTube.GetVideoUriAsync(videoID,YouTubeQuality.Quality720P);
				var uri = Android.Net.Uri.Parse(theURI.Uri.AbsoluteUri);
				videoView.SetVideoURI(uri);
				videoView.Start ();
				m_videoSourceSet = true;

				m_videoPregressTimer.Enabled = true;
				m_videoPregressTimer.Start();
			}
			catch (Exception ex) 
			{
				Console.WriteLine (ex.ToString ());
			}
		}

		void T_Elapsed (object sender, System.Timers.ElapsedEventArgs e)
		{
			RunOnUiThread(() =>
				{
					Console.WriteLine(videoView.CurrentPosition.ToString());
				});
				
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
				if (videoView.IsPlaying) {
					m_videoPregressTimer.Stop ();
					videoView.Pause ();
				} else if (m_videoSourceSet) {
					m_videoPregressTimer.Start ();
					videoView.Start ();
				}
			}	
		}					
	}
}


