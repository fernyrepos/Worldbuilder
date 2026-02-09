using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Dialog_FileSelector : Window
    {
        private string currentDirectoryPath;
        private List<string> drives;
        private Vector2 driveScrollPos;
        private Vector2 fileScrollPos;
        public Action<string> onSelectAction;
        public Dialog_FileSelector()
        {
            doCloseButton = true;
            doCloseX = true;
            forcePause = true;
            draggable = true;
            drives = new List<string>(Directory.GetLogicalDrives());
            currentDirectoryPath = drives[0];
        }

        public override Vector2 InitialSize => new Vector2(600f, 800f);

        public override void DoWindowContents(Rect inRect)
        {
            float maxDrivesPerRow = 5;
            float buttonHeight = 30f;
            float buttonSpacing = 5f;
            float drivePanelHeight = Mathf.Ceil(drives.Count / maxDrivesPerRow) * (buttonHeight + buttonSpacing);
            float pathPanelWidth = inRect.width;
            float pathPanelHeight = Text.CalcHeight("WB_FileSelectorCurrentPath".Translate() + currentDirectoryPath, pathPanelWidth) + 5f;

            float filePanelY = drivePanelHeight + pathPanelHeight + 20f;
            Rect drivePanel = new Rect(inRect.x, inRect.y, inRect.width, drivePanelHeight);
            DrawDrivePanel(drivePanel, maxDrivesPerRow, buttonHeight, buttonSpacing);
            Rect pathPanel = new Rect(inRect.x, inRect.y + drivePanelHeight + 10f, inRect.width, pathPanelHeight);
            DrawPathPanel(pathPanel);
            Rect filePanel = new Rect(inRect.x, inRect.y + filePanelY, inRect.width, inRect.height - filePanelY - 50);
            DrawFilePanel(filePanel);
        }

        private void DrawDrivePanel(Rect rect, float maxDrivesPerRow, float buttonHeight, float buttonSpacing)
        {
            float buttonWidth = (rect.width - (maxDrivesPerRow - 1) * buttonSpacing) / maxDrivesPerRow;
            float totalHeight = Mathf.Ceil(drives.Count / maxDrivesPerRow) * (buttonHeight + buttonSpacing);

            Rect scrollRect = new Rect(0, 0, rect.width - 16f, totalHeight);
            Widgets.BeginScrollView(rect, ref driveScrollPos, scrollRect);

            for (int i = 0; i < drives.Count; i++)
            {
                int row = i / (int)maxDrivesPerRow;
                int col = i % (int)maxDrivesPerRow;
                float xPos = col * (buttonWidth + buttonSpacing);
                float yPos = row * (buttonHeight + buttonSpacing);

                Rect driveButtonRect = new Rect(xPos, yPos, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(driveButtonRect, drives[i]))
                {
                    currentDirectoryPath = drives[i];
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawPathPanel(Rect rect)
        {
            Widgets.Label(rect, "WB_FileSelectorCurrentPath".Translate() + currentDirectoryPath);
        }

        private void DrawFilePanel(Rect rect)
        {
            float buttonHeight = 30f;
            float yPosition = 0f;
            float extraPadding = 10f;
            var directories = Directory.GetDirectories(currentDirectoryPath)
                                           .Where(d => (new DirectoryInfo(d).Attributes & FileAttributes.Hidden) == 0)
                                           .ToArray();

            var supportedFiles = Directory.GetFiles(currentDirectoryPath, "*.png")
                                         .Where(f => (new FileInfo(f).Attributes & FileAttributes.Hidden) == 0)
                                         .ToArray();
            float totalHeight = (directories.Length + supportedFiles.Length) * (buttonHeight + 5f) + extraPadding;
            if (Directory.GetParent(currentDirectoryPath) != null)
            {
                totalHeight += buttonHeight + 5f;
            }

            Rect scrollRect = new Rect(0, 0, rect.width - 16f, totalHeight);
            Widgets.BeginScrollView(rect, ref fileScrollPos, scrollRect);
            Text.Anchor = TextAnchor.MiddleLeft;
            if (Directory.GetParent(currentDirectoryPath) != null)
            {
                Rect upButtonRect = new Rect(0, yPosition, rect.width - 16f, buttonHeight);
                if (Widgets.ButtonText(upButtonRect, "WB_FileSelectorUp".Translate()))
                {
                    currentDirectoryPath = Directory.GetParent(currentDirectoryPath).FullName;
                }
                yPosition += buttonHeight + 5f;
            }
            foreach (var directory in directories)
            {
                Rect dirButtonRect = new Rect(0, yPosition, rect.width - 16f, buttonHeight);
                if (Widgets.ButtonText(dirButtonRect, Path.GetFileName(directory) + "/"))
                {
                    currentDirectoryPath = directory;
                }
                yPosition += buttonHeight + 5f;
            }
            foreach (var filePath in supportedFiles)
            {
                Rect fileButtonRect = new Rect(0, yPosition, rect.width - 16f, buttonHeight);
                if (Widgets.ButtonText(fileButtonRect, Path.GetFileName(filePath)))
                {
                    onSelectAction?.Invoke(filePath);
                    Close();
                }
                yPosition += buttonHeight + 5f;
            }
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.EndScrollView();
        }
    }
}