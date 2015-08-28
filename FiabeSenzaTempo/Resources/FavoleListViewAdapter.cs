using System;
using Android.Widget;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using System.Threading.Tasks;
using Android.Graphics;
using System.Net.Http;

namespace FiabeSenzaTempo
{
	public class FavoleListViewAdapter :BaseAdapter<videoItem>
	{
		private List<videoItem> m_theVideos = new List<videoItem>();

		private Context m_context;
		public FavoleListViewAdapter (Context context, List<videoItem> theVideos)
		{
			m_context = context;
			m_theVideos = theVideos;
		}
		#region implemented abstract members of BaseAdapter

		public override long GetItemId (int position)
		{
			return position;
		}
			
		public override Android.Views.View GetView (int position, View convertView, Android.Views.ViewGroup parent)
		{
			View row = convertView;
			if (row == null) {
				row = LayoutInflater.From(m_context).Inflate(Resource.Layout.FavoleListeViewItem,null,false);
			}
			TextView theTitle = (TextView)row.FindViewById (Resource.Id.textViewTitle);
			theTitle.Text = m_theVideos [position].Title.ToUpper();

			ImageView theImage = row.FindViewById<ImageView>(Resource.Id.imageViewTitle);
			theImage.SetImageBitmap(m_theVideos [position].Image);

			return row;	
		}

		public override int Count {
			get {
				return m_theVideos.Count;
			}
		}

		public override videoItem this [int index] {
			get {
				return m_theVideos [index];
			}
		}

		#endregion
	}
}

