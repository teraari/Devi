using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using AvaloniaEdit;
using Proofreader.Core.Models;

namespace Proofreader.App.Controls;

public class BindableTextEditor : TextEditor
{
    protected override Type StyleKeyOverride => typeof(TextEditor);

    private readonly SpellcheckColorizer _colorizer;
    private INotifyCollectionChanged? _observableErrors;

    public static readonly StyledProperty<string> DocumentTextProperty =
        AvaloniaProperty.Register<BindableTextEditor, string>(nameof(DocumentText), defaultValue: string.Empty);

    public string DocumentText
    {
        get => GetValue(DocumentTextProperty);
        set => SetValue(DocumentTextProperty, value);
    }

    public static readonly StyledProperty<IEnumerable<SpellingError>?> ErrorsProperty =
        AvaloniaProperty.Register<BindableTextEditor, IEnumerable<SpellingError>?>(nameof(Errors));

    public IEnumerable<SpellingError>? Errors
    {
        get => GetValue(ErrorsProperty);
        set => SetValue(ErrorsProperty, value);
    }

    public BindableTextEditor()
    {
        _colorizer = new SpellcheckColorizer();
        // ИСПРАВЛЕНИЕ: Используем TextArea.TextView
        TextArea.TextView.LineTransformers.Add(_colorizer);

        TextChanged += (_, _) => DocumentText = Text;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DocumentTextProperty)
        {
            var newText = change.NewValue as string ?? string.Empty;
            if (Text != newText)
            {
                var caretOffset = CaretOffset;
                Text = newText;
                CaretOffset = caretOffset;
            }
        }
        else if (change.Property == ErrorsProperty)
        {
            if (_observableErrors != null)
            {
                _observableErrors.CollectionChanged -= OnErrorsCollectionChanged;
            }

            if (change.NewValue is IEnumerable<SpellingError> errors)
            {
                _colorizer.Errors = errors.ToList();
                
                if (errors is INotifyCollectionChanged observable)
                {
                    _observableErrors = observable;
                    _observableErrors.CollectionChanged += OnErrorsCollectionChanged;
                }
            }
            else
            {
                _colorizer.Errors = Enumerable.Empty<SpellingError>();
            }
            
            // ИСПРАВЛЕНИЕ: Используем TextArea.TextView
            TextArea.TextView.Redraw();
        }
    }

    private void OnErrorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Errors != null)
        {
            _colorizer.Errors = Errors.ToList();
            // ИСПРАВЛЕНИЕ: Используем TextArea.TextView
            TextArea.TextView.Redraw();
        }
    }
}