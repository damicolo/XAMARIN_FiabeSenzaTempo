
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Gms.Ads;

namespace FiabeSenzaTempo
{
	[Activity (Label = "TheViewViewer", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]			
	public class TheViewViewer : Activity
	{
		private VideoView videoView;		
		private System.Timers.Timer m_videoPregressTimer;
		bool m_videoSourceSet = false;

		protected async override void OnCreate (Bundle bundle)
		{
			RequestWindowFeature(WindowFeatures.NoTitle);
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.VideoViewer);

			videoView = FindViewById<VideoView>(Resource.Id.videoViewer);
			videoView.Touch += videoView_Touch;
			videoView.Prepared += VideoView_Prepared;

			m_videoPregressTimer = new System.Timers.Timer ();
			m_videoPregressTimer.Interval = 500;
			m_videoPregressTimer.Elapsed += T_Elapsed;

			// advertising setup
			AdView mAdView = (AdView) this.FindViewById(Resource.Id.adView);
			AdRequest adRequest = new AdRequest.Builder ().Build ();
			mAdView.LoadAd(adRequest);

			string videoID = Intent.Extras.GetString ("videoID");
			try
			{
				YouTubeUri theURI = await  YouTube.GetVideoUriAsync(videoID,YouTubeQuality.Quality720P);
				var uri = Android.Net.Uri.Parse(theURI.Uri.AbsoluteUri);
				videoView.SetVideoURI(uri);
				videoView.Start ();
				m_videoPregressTimer.Enabled = true;
				m_videoPregressTimer.Start();
				m_videoSourceSet = true;
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


		void VideoView_Prepared (object sender, EventArgs e)
		{
			Console.WriteLine (videoView.Duration.ToString());
		}

	}


}

