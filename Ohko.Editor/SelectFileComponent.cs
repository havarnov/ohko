using System.IO;
using Apos.Gui;
using Apos.Input;
using FontStashSharp;
using Microsoft.Xna.Framework;

namespace Ohko.Editor;

public class SelectFileComponent(OhkoEditor editor)
{
    private string _selectedFile = string.Empty;
    public bool Enable { get; set; } = true;

    public void LoadContent()
    {
    }

    public void Update(GameTime gameTime)
    {
        if (!Enable)
        {
            return;
        }

        Dock.Put(0, 0, InputHelper.WindowWidth, InputHelper.WindowHeight);
        MenuPanel.Push();
        Textbox.Put(ref _selectedFile);
        var selectBtn = Button.Put("Select");
        if (selectBtn.Clicked)
        {
            if (_selectedFile != string.Empty && File.Exists(_selectedFile))
            {
                editor.SelectFile(_selectedFile);
            }
        }

        MenuPanel.Pop();
    }
}