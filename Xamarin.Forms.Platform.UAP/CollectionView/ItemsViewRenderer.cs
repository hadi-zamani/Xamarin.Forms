﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.UAP;
using UwpScrollBarVisibility = Windows.UI.Xaml.Controls.ScrollBarVisibility;
using UWPApp = Windows.UI.Xaml.Application;
using UWPDataTemplate = Windows.UI.Xaml.DataTemplate;

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
			else if(changedProperty.Is(ItemsView.HorizontalScrollBarVisibilityProperty))
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
					ListViewBase.Footer = new TextBlock { Text = text };
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
					Windows.UI.Xaml.Controls.ScrollBarVisibility.Auto);
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
			// TODO hartez 2018/06/05 16:18:57 Is there any performance benefit to caching the ItemsPanelTemplate lookup?	
			// TODO hartez 2018/05/29 15:38:04 Make sure the ItemsViewStyles.xaml xbf gets into the nuspec	
			var horizontalListView = new Windows.UI.Xaml.Controls.ListView()
			{
				ItemsPanel =
					(ItemsPanelTemplate)Windows.UI.Xaml.Application.Current.Resources["HorizontalListItemsPanel"]
			};

			ScrollViewer.SetHorizontalScrollMode(horizontalListView, ScrollMode.Auto);
			ScrollViewer.SetHorizontalScrollBarVisibility(horizontalListView,
				Windows.UI.Xaml.Controls.ScrollBarVisibility.Auto);

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

		async void ScrollToRequested(object sender, ScrollToRequestEventArgs args)
		{
			await ScrollTo(args);
		}

		object FindBoundItem(ScrollToRequestEventArgs args)
		{
			if (args.Mode == ScrollToMode.Position)
			{
				return _collectionViewSource.View[args.Index];
			}

			if (Element.ItemTemplate == null)
			{
				return args.Item;
			}

			for (int n = 0; n < _collectionViewSource.View.Count; n++)
			{
				if (_collectionViewSource.View[n] is ItemTemplateContext pair)
				{
					if (pair.Item == args.Item)
					{
						return _collectionViewSource.View[n];
					}
				}
			}

			return null;
		}

		async Task JumpTo(ListViewBase list, object targetItem, ScrollToPosition scrollToPosition)
		{
			var tcs = new TaskCompletionSource<object>();
			void ViewChanged(object s, ScrollViewerViewChangedEventArgs e) => tcs.TrySetResult(null);
			var scrollViewer = list.GetFirstDescendant<ScrollViewer>();

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
				else
				{
					// Center and End are going to be more complicated.
				}

				await tcs.Task;
			}
			finally
			{
				scrollViewer.ViewChanged -= ViewChanged;
			}

		}

		async Task ChangeViewAsync(ScrollViewer scrollViewer, double? horizontalOffset, double? verticalOffset, bool disableAnimation)
		{
			var tcs = new TaskCompletionSource<object>();
			void ViewChanged(object s, ScrollViewerViewChangedEventArgs e) => tcs.TrySetResult(null);

			try
			{
				scrollViewer.ViewChanged += ViewChanged;
				scrollViewer.ChangeView(horizontalOffset, verticalOffset, null, disableAnimation);
				await tcs.Task;
			}
			finally
			{
				scrollViewer.ViewChanged -= ViewChanged;
			}
		}

		async Task AnimateTo(ListViewBase list, object targetItem, ScrollToPosition scrollToPosition)
		{
			var scrollViewer = list.GetFirstDescendant<ScrollViewer>();

			var targetContainer = list.ContainerFromItem(targetItem) as UIElement;

			if (targetContainer == null)
			{
				var horizontalOffset = scrollViewer.HorizontalOffset;
				var verticalOffset = scrollViewer.VerticalOffset;

				await JumpTo(list, targetItem, scrollToPosition);
				targetContainer = list.ContainerFromItem(targetItem) as UIElement;
				await ChangeViewAsync(scrollViewer, horizontalOffset, verticalOffset, true);
			}

			if (targetContainer == null)
			{
				// Did not find the target item anywhere
				return;
			}

			// TODO hartez 2018/10/04 16:37:35 Okay, this sort of works for vertical lists but fails totally on horizontal lists. 
			var transform = targetContainer.TransformToVisual(scrollViewer.Content as UIElement);
			var position = transform?.TransformPoint(new Windows.Foundation.Point(0, 0));

			if (!position.HasValue)
			{
				return;
			}

			// TODO hartez 2018/10/05 17:23:23 The animated scroll works fine vertically if we are scrolling to a greater Y offset.	
			// If we're scrolling back up to a lower Y offset, it just gives up and sends us to 0 (first item)
			// Works fine if we disable animation, but that's not very helpful

			scrollViewer.ChangeView(position.Value.X, position.Value.Y, null, false);

			//if (scrollToPosition == ScrollToPosition.End)
			//{
			//	// Modify position
			//}
			//else if (scrollToPosition == ScrollToPosition.Center)
			//{
			//	// Modify position
			//}
			//else
			//{

			//}
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

		protected virtual async Task ScrollTo(ScrollToRequestEventArgs args)
		{
			if (!(Control is ListViewBase list))
			{
				return;
			}

			var targetItem = FindBoundItem(args);

			if (args.IsAnimated)
			{
				await AnimateTo(list, targetItem, args.ScrollToPosition);
			}
			else
			{
				await JumpTo(list, targetItem, args.ScrollToPosition);
			}
		}
	}
}