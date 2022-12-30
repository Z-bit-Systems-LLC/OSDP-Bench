using Android.Views;
using System.Collections.Specialized;
using System.Windows.Input;
using MvvmCross.IoC;
// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedMember.Global

namespace OSDPBench.UI.Android;

public class LinkerPleaseInclude
{
    public void Include(Button button)
    {
        button.Click += (_, _) => button.Text = button.Text + "";
    }

    public void Include(CheckBox checkBox)
    {
        checkBox.CheckedChange += (_, _) => checkBox.Checked = !checkBox.Checked;
    }

    public void Include(Switch @switch)
    {
        @switch.CheckedChange += (_, _) => @switch.Checked = !@switch.Checked;
    }

    public void Include(View view)
    {
        view.Click += (_, _) => view.ContentDescription = view.ContentDescription + "";
    }

    public void Include(TextView text)
    {
        text.TextChanged += (_, _) => text.Text = "" + text.Text;
        text.Hint = "" + text.Hint;
    }

    public void Include(CheckedTextView text)
    {
        text.TextChanged += (_, _) => text.Text = "" + text.Text;
        text.Hint = "" + text.Hint;
    }

    public void Include(CompoundButton cb)
    {
        cb.CheckedChange += (_, _) => cb.Checked = !cb.Checked;
    }

    public void Include(SeekBar sb)
    {
        sb.ProgressChanged += (_, _) => sb.Progress = sb.Progress + 1;
    }

    public void Include(Activity act)
    {
        act.Title = act.Title + "";
    }

    public void Include(INotifyCollectionChanged changed)
    {
        changed.CollectionChanged += (_, e) =>
        {
            var test = $"{e.Action}{e.NewItems}{e.NewStartingIndex}{e.OldItems}{e.OldStartingIndex}";
        };
    }

    public void Include(ICommand command)
    {
        command.CanExecuteChanged += (_, _) =>
        {
            if (command.CanExecute(null)) command.Execute(null);
        };
    }

    public void Include(MvxPropertyInjector injector)
    {
        injector = new MvxPropertyInjector();
    }

    public void Include(System.ComponentModel.INotifyPropertyChanged changed)
    {
        changed.PropertyChanged += (_, e) =>
        {
            var test = e.PropertyName;
        };
    }
}

