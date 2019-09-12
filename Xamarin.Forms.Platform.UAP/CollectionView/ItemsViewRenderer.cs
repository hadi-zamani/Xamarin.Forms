using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Xamarin.Forms.Platform.UAP;
using UwpScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollBarVisibility;
using UWPApp = Windows.UI.Xaml.Application;
using UWPDataTemplate = Windows.UI.Xaml.DataTemplate;
using UWPPoint = Windows.Foundation.Point;
using UWPSize = Windows.Foundation.Size;

namespace Xamarin.Forms.Platform.UWP
{
	public class ItemsViewRenderer : ViewRenderer<ItemsView, ListViewBase>
	{
		IItemsLayout _layout;
		CollectionViewSource _collectionViewSource;

		protected ListViewBase ListViewBase { get; private set; }
		UwpScrollBarVisibility? _defaultHorizontalScrollVisibility;
		UwpScrollBarVisibility? _defaultVerticalScrollVisibility;

		UWPDataTemplate ViewTemplate => (UWPDataTemplate)UWPApp.Current.Resources["View"];
		UWPDataTemplate ItemsViewTemplate => (UWPDataTemplate)UWPApp.Current.Resources["ItemsViewDefaultTemplate"];

		View _currentHeader;
		View _currentFooter;

		ItemsLayoutOrientation Orientation => (_layout as ListItemsLayout)?.Orientation ?? ItemsLayoutOrientation.Vertical;

		static UWPPoint Zero = new UWPPoint(0, 0);

		protected ItemsControl ItemsControl { get; private set; }

		protected override void OnElementChanged(ElementChangedEventArgs<ItemsView> args)
		{
			base.OnElementChanged(args);
			TearDownOldElement(args.OldElement);
			SetUpNewElement(args.NewElement);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs changedProperty)
		{
			base.OnElementPropertyChanged(sender, changedProperty);

			if (changedProperty.Is(ItemsView.ItemsSourceProperty))
			{
				UpdateItemsSource();
			}
			else if (changedProperty.Is(ItemsView.ItemTemplateProperty))
			{
				UpdateItemTemplate();
			}
			else if (changedProperty.Is(ItemsView.HorizontalScrollBarVisibilityProperty))
			{
				UpdateHorizontalScrollBarVisibility();
			}
			else if (changedProperty.Is(ItemsView.VerticalScrollBarVisibilityProperty))
			{
				UpdateVerticalScrollBarVisibility();
			}
			else if (changedProperty.IsOneOf(ItemsView.HeaderProperty, ItemsView.HeaderTemplateProperty))
			{
				UpdateHeader();
			}
			else if (changedProperty.IsOneOf(ItemsView.FooterProperty, ItemsView.FooterTemplateProperty))
			{
				UpdateFooter();
			}
		}

		protected virtual ListViewBase SelectLayout(IItemsLayout layoutSpecification)
		{
			switch (layoutSpecification)
			{
				case GridItemsLayout gridItemsLayout:
					return CreateGridView(gridItemsLayout);
				case ListItemsLayout listItemsLayout
					when listItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal:
					return CreateHorizontalListView();
			}

			// Default to a plain old vertical ListView
			return new Windows.UI.Xaml.Controls.ListView();
		}

		protected virtual void UpdateItemsSource()
		{
			if (ListViewBase == null)
			{
				return;
			}

			// TODO hartez 2018-05-22 12:59 PM Handle grouping

			var itemsSource = Element.ItemsSource;

			if (itemsSource == null)
			{
				_collectionViewSource = null;
				return;
			}

			var itemTemplate = Element.ItemTemplate;

			if (_collectionViewSource != null)
			{
				if (_collectionViewSource.Source is ObservableItemTemplateCollection observableItemTemplateCollection)
				{
					observableItemTemplateCollection.CleanUp();
				}
			}

			if (itemTemplate != null)
			{
				_collectionViewSource = new CollectionViewSource
				{
					Source = TemplatedItemSourceFactory.Create(itemsSource, itemTemplate, Element),
					IsSourceGrouped = false
				};
			}
			else
			{
				_collectionViewSource = new CollectionViewSource
				{
					Source = itemsSource,
					IsSourceGrouped = false
				};
			}

			ListViewBase.ItemsSource = _collectionViewSource.View;
		}

		protected virtual void UpdateItemTemplate()
		{
			if (Element == null || ListViewBase == null)
			{
				return;
			}

			ListViewBase.ItemTemplate = Element.ItemTemplate == null ? null : ItemsViewTemplate;

			UpdateItemsSource();
		}

		protected virtual void UpdateHeader()
		{
			if (ListViewBase == null)
			{
				return;
			}

			if (_currentHeader != null)
			{
				Element.RemoveLogicalChild(_currentHeader);
				_currentHeader = null;
			}

			var header = Element.Header;

			switch (header)
			{
				case null:
					ListViewBase.Header = null;
					break;

				case string text:
					ListViewBase.HeaderTemplate = null;
					ListViewBase.Header = new TextBlock { Text = text };
					break;

				case View view:
					ListViewBase.HeaderTemplate = ViewTemplate;
					_currentHeader = view;
					Element.AddLogicalChild(_currentHeader);
					ListViewBase.Header = view;
					break;

				default:
					var headerTemplate = Element.HeaderTemplate;
					if (headerTemplate != null)
					{
						ListViewBase.HeaderTemplate = ItemsViewTemplate;
						ListViewBase.Header = new ItemTemplateContext(headerTemplate, header, Element);
					}
					else
					{
						ListViewBase.HeaderTemplate = null;
						ListViewBase.Header = null;
					}
					break;
			}
		}

		protected virtual void UpdateFooter()
		{
			if (ListViewBase == null)
			{
				return;
			}

			if (_currentFooter != null)
			{
				Element.RemoveLogicalChild(_currentFooter);
				_currentFooter = null;
			}

			var footer = Element.Footer;

			switch (footer)
			{
				case null:
					ListViewBase.Footer = null;
					break;

				case string text:
					ListViewBase.FooterTemplate = null;
					ListViewBase.Footer = new TextBlock
					{
						Text = text
					};
					break;
				case View view:
					ListViewBase.FooterTemplate = ViewTemplate;
					_currentFooter = view;
					Element.AddLogicalChild(_currentFooter);
					ListViewBase.Footer = view;
					break;

				default:
					var footerTemplate = Element.FooterTemplate;
					if (footerTemplate != null)
					{
						ListViewBase.FooterTemplate = ItemsViewTemplate;
						ListViewBase.Footer = new ItemTemplateContext(footerTemplate, footer, Element);
					}
					else
					{
						ListViewBase.FooterTemplate = null;
						ListViewBase.Footer = null;
					}
					break;
			}
		}

		static ListViewBase CreateGridView(GridItemsLayout gridItemsLayout)
		{
			var gridView = new FormsGridView();

			if (gridItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal)
			{
				gridView.UseHorizontalItemsPanel();

				// TODO hartez 2018/06/06 12:13:38 Should this logic just be built into FormsGridView?	
				ScrollViewer.SetHorizontalScrollMode(gridView, ScrollMode.Auto);
				ScrollViewer.SetHorizontalScrollBarVisibility(gridView,
					UwpScrollBarVisibility.Auto);
			}
			else
			{
				gridView.UseVerticalalItemsPanel();
			}

			gridView.MaximumRowsOrColumns = gridItemsLayout.Span;

			return gridView;
		}

		static ListViewBase CreateHorizontalListView()
		{
			var horizontalListView = new Windows.UI.Xaml.Controls.ListView()
			{
				ItemsPanel =
					(ItemsPanelTemplate)UWPApp.Current.Resources["HorizontalListItemsPanel"]
			};

			ScrollViewer.SetHorizontalScrollMode(horizontalListView, ScrollMode.Auto);
			ScrollViewer.SetHorizontalScrollBarVisibility(horizontalListView,
				UwpScrollBarVisibility.Auto);

			return horizontalListView;
		}

		void LayoutPropertyChanged(object sender, PropertyChangedEventArgs property)
		{
			if (property.Is(GridItemsLayout.SpanProperty))
			{
				if (ListViewBase is FormsGridView formsGridView)
				{
					formsGridView.MaximumRowsOrColumns = ((GridItemsLayout)_layout).Span;
				}
			}
		}

		protected virtual void SetUpNewElement(ItemsView newElement)
		{
			if (newElement == null)
			{
				return;
			}

			if (ListViewBase == null)
			{
				ListViewBase = SelectLayout(newElement.ItemsLayout);
				ListViewBase.IsSynchronizedWithCurrentItem = false;

				_layout = newElement.ItemsLayout;
				_layout.PropertyChanged += LayoutPropertyChanged;

				SetNativeControl(ListViewBase);
			}

			UpdateItemTemplate();
			UpdateItemsSource();
			UpdateHeader();
			UpdateFooter();
			UpdateVerticalScrollBarVisibility();
			UpdateHorizontalScrollBarVisibility();

			// Listen for ScrollTo requests
			newElement.ScrollToRequested += ScrollToRequested;
		}

		protected virtual void TearDownOldElement(ItemsView oldElement)
		{
			if (oldElement == null)
			{
				return;
			}

			if (_layout != null)
			{
				// Stop tracking the old layout
				_layout.PropertyChanged -= LayoutPropertyChanged;
				_layout = null;
			}

			// Stop listening for ScrollTo requests
			oldElement.ScrollToRequested -= ScrollToRequested;
		}

		protected virtual async Task ScrollTo(ScrollToRequestEventArgs args)
		{
			if (!(Control is ListViewBase list))
			{
				return;
			}

			var (position, item) = FindBoundItem(args);

			if (args.IsAnimated)
			{
				await AnimateTo(list, item, args.ScrollToPosition);
			}
			else
			{
				await JumpToItemAsync(list, item, args.ScrollToPosition);
			}
		}

		async void ScrollToRequested(object sender, ScrollToRequestEventArgs args)
		{
			await ScrollTo(args);
		}

		(int, object) FindBoundItem(ScrollToRequestEventArgs args)
		{
			if (args.Mode == ScrollToMode.Position)
			{
				return (args.Index, _collectionViewSource.View[args.Index]);
			}

			if (Element.ItemTemplate == null)
			{
				return (-1, args.Item);
			}

			for (int n = 0; n < _collectionViewSource.View.Count; n++)
			{
				if (_collectionViewSource.View[n] is ItemTemplateContext pair)
				{
					if (pair.Item == args.Item)
					{
						return (n, _collectionViewSource.View[n]);
					}
				}
			}

			return (-1, null);
		}

		async Task JumpToItemAsync(ListViewBase list, object targetItem, ScrollToPosition scrollToPosition)
		{
			var scrollViewer = list.GetFirstDescendant<ScrollViewer>();

			var tcs = new TaskCompletionSource<object>();
			Func<Task> adjust = null;

			async void ViewChanged(object s, ScrollViewerViewChangedEventArgs e)
			{
				if (e.IsIntermediate)
				{
					return;
				}

				scrollViewer.ViewChanged -= ViewChanged;

				if (adjust != null)
				{
					await adjust();
				}

				tcs.TrySetResult(null);
			}

			try
			{
				scrollViewer.ViewChanged += ViewChanged;

				if (scrollToPosition == ScrollToPosition.Start)
				{
					list.ScrollIntoView(targetItem, ScrollIntoViewAlignment.Leading);
				}
				else if (scrollToPosition == ScrollToPosition.MakeVisible)
				{
					list.ScrollIntoView(targetItem, ScrollIntoViewAlignment.Default);
				}
				else if (scrollToPosition == ScrollToPosition.End)
				{
					list.ScrollIntoView(targetItem, ScrollIntoViewAlignment.Leading);

					adjust = async () =>
					{
						var point = new UWPPoint(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
						var targetContainer = list.ContainerFromItem(targetItem) as UIElement;
						point = AdjustToEnd(point, targetContainer.DesiredSize, scrollViewer, Orientation);
						await JumpToOffsetAsync(scrollViewer, point.X, point.Y);
					};
				}
				else if (scrollToPosition == ScrollToPosition.Center)
				{
					list.ScrollIntoView(targetItem, ScrollIntoViewAlignment.Leading);

					adjust = async () =>
					{
						var point = new UWPPoint(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
						var targetContainer = list.ContainerFromItem(targetItem) as UIElement;
						point = AdjustToCenter(point, targetContainer.DesiredSize, scrollViewer, Orientation);
						await JumpToOffsetAsync(scrollViewer, point.X, point.Y);
					};
				}

				await tcs.Task;
			}
			finally
			{
				scrollViewer.ViewChanged -= ViewChanged;
			}
		}

		async Task JumpToOffsetAsync(ScrollViewer scrollViewer, double targetHorizontalOffset, double targetVerticalOffset)
		{
			var tcs = new TaskCompletionSource<object>();

			void ViewChanged(object s, ScrollViewerViewChangedEventArgs e)
			{
				tcs.TrySetResult(null);
			}

			try
			{
				scrollViewer.ViewChanged += ViewChanged;
				scrollViewer.ChangeView(targetHorizontalOffset, targetVerticalOffset, null, true);
				await tcs.Task;
			}
			finally
			{
				scrollViewer.ViewChanged -= ViewChanged;
			}
		}

		async Task<bool> AnimateToOffsetAsync(ScrollViewer scrollViewer, double targetHorizontalOffset, double targetVerticalOffset, 
			Func<Task<bool>> interruptCheck = null)
		{
			var tcs = new TaskCompletionSource<bool>();

			async void ViewChanged(object s, ScrollViewerViewChangedEventArgs e)
			{
				if (tcs.Task.IsCompleted)
				{
					return;
				}

				if (e.IsIntermediate)
				{
					if (interruptCheck != null)
					{

						if (await interruptCheck())
						{
							scrollViewer.ChangeView(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset, 1.0f, true);
							tcs.TrySetResult(true);
						}
					}
				}
				else
				{
					if (scrollViewer.HorizontalOffset != targetHorizontalOffset
						|| scrollViewer.VerticalOffset != targetVerticalOffset)
					{
						tcs.TrySetResult(false);
					}
					else
					{
						tcs.TrySetResult(true);
					}
				}
			}

			try
			{
				scrollViewer.ViewChanged += ViewChanged;
				scrollViewer.ChangeView(targetHorizontalOffset, targetVerticalOffset, null, false);
				return await tcs.Task;
			}
			finally
			{
				scrollViewer.ViewChanged -= ViewChanged;
			}
		}

		async Task ScrollToTargetContainer(UIElement targetContainer, ScrollViewer scrollViewer, ScrollToPosition scrollToPosition)
		{
			var transform = targetContainer.TransformToVisual(scrollViewer.Content as UIElement);
			var position = transform?.TransformPoint(Zero);
			
			if (!position.HasValue)
			{
				return;
			}

			UWPPoint offset = position.Value;
			var itemSize = targetContainer.DesiredSize;

			// The transform will put the container at the top of the ScrollViewer; we'll need to adjust for
			// other scroll positions

			switch (scrollToPosition)
			{
				case ScrollToPosition.MakeVisible:
					AdjustToMakeVisible(offset, itemSize, scrollViewer, Orientation);
					break;
				case ScrollToPosition.Center:
					offset = AdjustToCenter(offset, itemSize, scrollViewer, Orientation);
					break;
				case ScrollToPosition.End:
					offset = AdjustToEnd(offset, itemSize, scrollViewer, Orientation);
					break;
				case ScrollToPosition.Start:
					// Already done
					break;
			}

			await AnimateToOffsetAsync(scrollViewer, offset.X, offset.Y);
		}

		UWPPoint AdjustToMakeVisible(UWPPoint point, UWPSize itemSize, ScrollViewer scrollViewer, ItemsLayoutOrientation orientation)
		{
			// If we're scrolling down/right, we want the item to show up at the end of the viewport;
			// otherwise, we want it at the top of the viewport

			if (orientation == ItemsLayoutOrientation.Horizontal)
			{
				if (point.X > (scrollViewer.HorizontalOffset + scrollViewer.ViewportWidth))
				{
					return AdjustToEnd(point, itemSize, scrollViewer, orientation);
				}

				if (point.X >= scrollViewer.HorizontalOffset && point.X < (scrollViewer.HorizontalOffset + scrollViewer.ViewportWidth - itemSize.Width))
				{
					// The target is already in the viewport, no reason to scroll at all
					return new UWPPoint(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
				}

				return point;
			}

			if (point.Y > (scrollViewer.VerticalOffset + scrollViewer.ViewportHeight))
			{
				return AdjustToEnd(point, itemSize, scrollViewer, orientation);
			}

			if (point.Y >= scrollViewer.VerticalOffset && point.Y < (scrollViewer.VerticalOffset + scrollViewer.ViewportHeight - itemSize.Height))
			{
				// The target is already in the viewport, no reason to scroll at all
				return new UWPPoint(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
			}

			return point;
		}

		UWPPoint AdjustToEnd(UWPPoint point, UWPSize itemSize, ScrollViewer scrollViewer, ItemsLayoutOrientation orientation)
		{
			double adjustment;

			if (orientation == ItemsLayoutOrientation.Horizontal)
			{
				adjustment = scrollViewer.ViewportWidth - itemSize.Width;
				return new UWPPoint(point.X - adjustment, point.Y);
			}

			adjustment = scrollViewer.ViewportHeight - itemSize.Height;
			return new UWPPoint(point.X, point.Y - adjustment);
		}

		UWPPoint AdjustToCenter(UWPPoint point, UWPSize itemSize, ScrollViewer scrollViewer, ItemsLayoutOrientation orientation)
		{
			double adjustment;

			if (orientation == ItemsLayoutOrientation.Horizontal)
			{
				adjustment = (scrollViewer.ViewportWidth / 2) - (itemSize.Width / 2);
				return new UWPPoint(point.X - adjustment, point.Y);
			}

			adjustment = (scrollViewer.ViewportHeight / 2) - (itemSize.Height / 2);
			return new UWPPoint(point.X, point.Y - adjustment);
		}

		async Task<bool> ScrollToItem(ListViewBase list, object targetItem, ScrollViewer scrollViewer, ScrollToPosition scrollToPosition)
		{
			var targetContainer = list.ContainerFromItem(targetItem) as UIElement;

			if (targetContainer != null)
			{
				await ScrollToTargetContainer(targetContainer, scrollViewer, scrollToPosition);
				return true;
			}

			return false;
		}

		async Task<UWPPoint> GetApproximateTarget(ListViewBase list, object targetItem, ScrollToPosition scrollToPosition)
		{
			var scrollViewer = list.GetFirstDescendant<ScrollViewer>();

			var horizontalOffset = scrollViewer.HorizontalOffset;
			var verticalOffset = scrollViewer.VerticalOffset;

			await JumpToItemAsync(list, targetItem, ScrollToPosition.Start);
			var targetContainer = list.ContainerFromItem(targetItem) as UIElement;
			var transform = targetContainer.TransformToVisual(scrollViewer.Content as UIElement);
			await JumpToOffsetAsync(scrollViewer, horizontalOffset, verticalOffset);

			return transform.TransformPoint(Zero);
		}

		async Task AnimateTo(ListViewBase list, object targetItem, ScrollToPosition scrollToPosition)
        {
			var scrollViewer = list.GetFirstDescendant<ScrollViewer>();

			if (await ScrollToItem(list, targetItem, scrollViewer, scrollToPosition))
			{
				// Happy path; the item was already realized and we could just scroll to it
				return;
			}
			
			var targetPoint = await GetApproximateTarget(list, targetItem, scrollToPosition);
			
			await AnimateToOffsetAsync(scrollViewer, targetPoint.X, targetPoint.Y,
				async () => await ScrollToItem(list, targetItem, scrollViewer, scrollToPosition));
		}

		void UpdateVerticalScrollBarVisibility()
		{
			if (_defaultVerticalScrollVisibility == null)
				_defaultVerticalScrollVisibility = ScrollViewer.GetVerticalScrollBarVisibility(Control);

			switch (Element.VerticalScrollBarVisibility)
			{
				case (ScrollBarVisibility.Always):
					ScrollViewer.SetVerticalScrollBarVisibility(Control, UwpScrollBarVisibility.Visible);
					break;
				case (ScrollBarVisibility.Never):
					ScrollViewer.SetVerticalScrollBarVisibility(Control, UwpScrollBarVisibility.Hidden);
					break;
				case (ScrollBarVisibility.Default):
					ScrollViewer.SetVerticalScrollBarVisibility(Control, _defaultVerticalScrollVisibility.Value);
					break;
			}
		}

		void UpdateHorizontalScrollBarVisibility()
		{
			if (_defaultHorizontalScrollVisibility == null)
				_defaultHorizontalScrollVisibility = ScrollViewer.GetHorizontalScrollBarVisibility(Control);

			switch (Element.HorizontalScrollBarVisibility)
			{
				case (ScrollBarVisibility.Always):
					ScrollViewer.SetHorizontalScrollBarVisibility(Control, UwpScrollBarVisibility.Visible);
					break;
				case (ScrollBarVisibility.Never):
					ScrollViewer.SetHorizontalScrollBarVisibility(Control, UwpScrollBarVisibility.Hidden);
					break;
				case (ScrollBarVisibility.Default):
					ScrollViewer.SetHorizontalScrollBarVisibility(Control, _defaultHorizontalScrollVisibility.Value);
					break;
			}
		}
	}
}