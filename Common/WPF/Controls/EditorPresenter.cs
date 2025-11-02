using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Common.WPF.Controls
{
	public class EditorPresenter : Control
	{
		static EditorPresenter()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(EditorPresenter), new FrameworkPropertyMetadata(typeof(EditorPresenter)));
		}

	    public static DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(EditorPresenter), new FrameworkPropertyMetadata(false));
		public bool IsEditing
		{
			get { return (bool)GetValue(IsEditingProperty); }
			private set
			{
			    bool val = IsEditing;
			    SetValue(IsEditingProperty, value);
			    if (val != IsEditing)
			    {
			        if (IsEditing && EditTemplate != null && IsTemplateEditable)
			            Template = EditTemplate;
			        else if (!IsEditing)
			            Template = ViewTemplate;
			    }
            }
		}

        //Добавлено свойство, позволяющее блокировать ввод
        public static DependencyProperty EditableTemplateProperty = DependencyProperty.Register("IsTemplateEditable", typeof(bool), typeof(EditorPresenter), new FrameworkPropertyMetadata(true));

        public bool IsTemplateEditable
        {
            get { return (bool)GetValue(EditableTemplateProperty); }
            set
            {
                SetValue(EditableTemplateProperty, value);
            }
        }

	    public static DependencyProperty ToggleableTemplateProperty = DependencyProperty.Register("IsTemplateToggleable", typeof(bool), typeof(EditorPresenter), new FrameworkPropertyMetadata(true));

	    public bool IsTemplateToggleable
        {
	        get { return (bool)GetValue(ToggleableTemplateProperty); }
	        set
	        {
	            SetValue(ToggleableTemplateProperty, value);
	        }
	    }

        public static DependencyProperty EditTemplateProperty = DependencyProperty.Register("EditTemplate", typeof(ControlTemplate), typeof(EditorPresenter), new FrameworkPropertyMetadata());
		public ControlTemplate EditTemplate
		{
			get { return (ControlTemplate)GetValue(EditTemplateProperty); }
			set { SetValue(EditTemplateProperty, value); }
		}

		public static DependencyProperty ViewTemplateProperty = DependencyProperty.Register("ViewTemplate", typeof(ControlTemplate), typeof(EditorPresenter), new FrameworkPropertyMetadata(ViewTemplateChanged));
		public ControlTemplate ViewTemplate
		{
			get { return (ControlTemplate)GetValue(ViewTemplateProperty); }
			set { SetValue(ViewTemplateProperty, value); }
		}

		bool _canBeEdit;
		bool _isMouseWithinScope;
		TreeViewItem _viewItem;

		public EditorPresenter()
		{
			IsKeyboardFocusWithinChanged += OnEditorIsKeyboardFocusWithinChanged;
			LostKeyboardFocus += OnEditorLostKeyboardFocus;
			LayoutUpdated += OnEditorLayoutUpdated;
			KeyDown += OnEditorKeyDown;
			FocusVisualStyle = null;
			IsTabStop = false;
		}

		static void ViewTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			EditorPresenter presenter = (EditorPresenter)d;
			presenter.Template = (ControlTemplate)e.NewValue;
			presenter.IsEditing = false;
		}

		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			if (!IsEditing && IsParentSelected)
			{
				_canBeEdit = true;
			}
		}
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			_isMouseWithinScope = false;
			_canBeEdit = false;
		}
		protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
		{
			OnMouseUp(e);

			if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Middle)
				return;
			if (!IsEditing && IsTemplateToggleable)
			{
				if (!e.Handled && (_canBeEdit || _isMouseWithinScope))
					IsEditing = true;

				if (IsParentSelected)
					_isMouseWithinScope = true;
			}
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			if (IsEditing)
				Focus();
			if (_viewItem == null)
				_viewItem = GetDependencyObjectFromVisualTree(this, typeof(TreeViewItem)) as TreeViewItem;
		}
		void OnEditorLayoutUpdated(object sender, EventArgs e)
		{
			if (IsEditing && !IsKeyboardFocusWithin)
				Focus();
		}
		void OnEditorKeyDown(object sender, KeyEventArgs e)
		{
			if (IsEditing && (e.Key == Key.Enter || e.Key == Key.F2))
			{
				IsEditing = false;
				_canBeEdit = false;
			}
		}
		void OnEditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (!IsKeyboardFocusWithin)
				IsEditing = false;
		}
		void OnEditorIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.OldValue && (bool)e.NewValue)
				IsEditing = false;
		}

		bool IsParentSelected
		{
			get
			{
				if (_viewItem == null)
					return true;
				return _viewItem.IsSelected;
			}
		}

		DependencyObject GetDependencyObjectFromVisualTree(DependencyObject startObject, Type type)
		{
			//Iterate the visual tree to get the parent(ItemsControl) of this control
			DependencyObject parent = startObject;
			while (parent != null)
			{
				if (type.IsInstanceOfType(parent))
					break;
				parent = VisualTreeHelper.GetParent(parent);
			}
			return parent;
		}
	}
}