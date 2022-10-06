using Android.OS;
using Android.Views;
using Fragment = AndroidX.Fragment.App.Fragment;

namespace TooGoodToGoNotifierAndroidApp.Fragments
{
    public class ProductsFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_products, container, false);

            return view;
        }
    }
}