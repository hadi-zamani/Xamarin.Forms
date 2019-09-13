using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;

#if UITEST
using Xamarin.Forms.Core.UITests;
using Xamarin.UITest;
using NUnit.Framework;
#endif

namespace Xamarin.Forms.Controls.Issues
{
#if UITEST
	[Category(UITestCategories.ManualReview)]
#endif
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Github, 6384, "content page in tabbed page not showing inside shell tab", PlatformAffected.Default)]
	public class Github6384 : TestShell
	{
		protected override void Init()
		{
			// Initialize ui here instead of ctor
			var tabOneButton = new Button
			{
				AutomationId = "NavigationButton",
				Text = "Push me!"
			};

			tabOneButton.Clicked += TabOneButton_Clicked;

			var tabOnePage = new ContentPage { Content = tabOneButton };

			var tabTwoPage = new ContentPage { Content = new Label { Text = "Go to TabOne" } };
			var tabOne = new Tab { Title = "TabOne" };
			var tabTwo = new Tab { Title = "TabTwo" };
			tabOne.Items.Add(tabOnePage);
			tabTwo.Items.Add(tabTwoPage);

			this.Items.Add(
					new TabBar
					{
						Items = { tabOne, tabTwo }
					}
			);
		}

		private void TabOneButton_Clicked(object sender, System.EventArgs e)
		{
			var subTabPageOne = new ContentPage
			{
				Content = new Label
				{
					Text = "See me?",
					VerticalTextAlignment = TextAlignment.Center,
				},
				BackgroundColor = Color.Blue
			};
			var subTabPageTwo = new ContentPage
			{
				Content = new Label
				{
					Text = "See me?",
					VerticalTextAlignment = TextAlignment.Center,
				},
				BackgroundColor = Color.Green
			};

			var tabbedPage = new TabbedPage { Title = "TabbedPage" };
			tabbedPage.Children.Add(subTabPageOne);
			tabbedPage.Children.Add(subTabPageTwo);
			tabbedPage.BackgroundColor = Color.Red;
			Shell.SetTabBarIsVisible(tabbedPage, false);
			this.Navigation.PushAsync(tabbedPage);

		}

#if UITEST
		[Test]
		public void Issue1Test() 
		{
			// Delete this and all other UITEST sections if there is no way to automate the test. Otherwise, be sure to rename the test and update the Category attribute on the class. Note that you can add multiple categories.
			RunningApp.Screenshot("I am at Issue1");	
			RunningApp.WaitForElement(q => q.Marked("Issue1Label"));
			RunningApp.Screenshot("I see the Label");
		}
#endif
	}

	[Preserve(AllMembers = true)]
	public class ViewModelGithub6384
	{
		public ViewModelGithub6384()
		{

		}
	}

	[Preserve(AllMembers = true)]
	public class ModelGithub6384
	{
		public ModelGithub6384()
		{

		}
	}
}
