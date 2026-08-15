using DevExpress.XtraBars;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraLayout;
using DevExpress.XtraNavBar;
using DevExpress.XtraRichEdit;
using DevExpress.XtraTab;
using DevExpress.XtraTreeList;
using DevExpress.XtraWaitForm;
using System.Drawing;
using System.Windows.Forms;

namespace Nexus_Launcher.Helpers
{
    internal static class FontManager
    {
        public static void ApplyFont(
            Control parent,
            string fontFamily)
        {
            if (string.IsNullOrWhiteSpace(fontFamily))
                return;

            Apply(parent, fontFamily);
        }

        private static Font CloneFont(
            Font original,
            string family)
        {
            if (original == null)
            {
                return new Font(
                    family,
                    9F,
                    FontStyle.Regular);
            }

            return new Font(
                family,
                original.Size,
                original.Style,
                original.Unit);
        }

        private static void Apply(
            Control control,
            string fontFamily)
        {
            //----------------------------------------
            // Standard WinForms
            //----------------------------------------

            control.Font =
                CloneFont(
                    control.Font,
                    fontFamily);

            //----------------------------------------
            // XtraEditors
            //----------------------------------------

            BaseEdit edit =
                control as BaseEdit;

            if (edit != null)
            {
                edit.Properties.Appearance.Font =
                    CloneFont(
                        edit.Properties.Appearance.Font,
                        fontFamily);

                edit.Properties.Appearance.Options.UseFont = true;
            }

            //----------------------------------------
            // LabelControl
            //----------------------------------------

            LabelControl label =
                control as LabelControl;

            if (label != null)
            {
                label.Appearance.Font =
                    CloneFont(
                        label.Appearance.Font,
                        fontFamily);

                label.Appearance.Options.UseFont = true;
            }

            //----------------------------------------
            // SimpleButton
            //----------------------------------------

            SimpleButton button =
                control as SimpleButton;

            if (button != null)
            {
                button.Appearance.Font =
                    CloneFont(
                        button.Appearance.Font,
                        fontFamily);

                button.Appearance.Options.UseFont = true;
            }

            //----------------------------------------
            // Ribbon
            //----------------------------------------

            RibbonControl ribbon =
                control as RibbonControl;

            if (ribbon != null)
            {
                foreach (BarItem item in ribbon.Items)
                {
                    item.ItemAppearance.Normal.Font =
                        CloneFont(
                            item.ItemAppearance.Normal.Font,
                            fontFamily);

                    item.ItemAppearance.Normal.Options.UseFont = true;

                    item.ItemAppearance.Disabled.Font =
                        CloneFont(
                            item.ItemAppearance.Disabled.Font,
                            fontFamily);

                    item.ItemAppearance.Disabled.Options.UseFont = true;

                    item.ItemAppearance.Hovered.Font =
                        CloneFont(
                            item.ItemAppearance.Hovered.Font,
                            fontFamily);

                    item.ItemAppearance.Hovered.Options.UseFont = true;

                    item.ItemAppearance.Pressed.Font =
                        CloneFont(
                            item.ItemAppearance.Pressed.Font,
                            fontFamily);

                    item.ItemAppearance.Pressed.Options.UseFont = true;
                }
            }

            //----------------------------------------
            // GridControl
            //----------------------------------------

            GridControl grid =
                control as GridControl;

            if (grid != null)
            {
                foreach (BaseView view in grid.ViewCollection)
                {
                    GridView gv =
                        view as GridView;

                    if (gv == null)
                        continue;

                    gv.Appearance.Row.Font =
                        CloneFont(
                            gv.Appearance.Row.Font,
                            fontFamily);

                    gv.Appearance.HeaderPanel.Font =
                        CloneFont(
                            gv.Appearance.HeaderPanel.Font,
                            fontFamily);

                    gv.Appearance.FooterPanel.Font =
                        CloneFont(
                            gv.Appearance.FooterPanel.Font,
                            fontFamily);

                    gv.Appearance.GroupRow.Font =
                        CloneFont(
                            gv.Appearance.GroupRow.Font,
                            fontFamily);

                    gv.Appearance.FilterPanel.Font =
                        CloneFont(
                            gv.Appearance.FilterPanel.Font,
                            fontFamily);

                    gv.Appearance.Row.Options.UseFont = true;
                    gv.Appearance.HeaderPanel.Options.UseFont = true;
                    gv.Appearance.FooterPanel.Options.UseFont = true;
                    gv.Appearance.GroupRow.Options.UseFont = true;
                    gv.Appearance.FilterPanel.Options.UseFont = true;
                }
            }

            //----------------------------------------
            // TreeList
            //----------------------------------------

            TreeList tree =
                control as TreeList;

            if (tree != null)
            {
                tree.Appearance.Row.Font =
                    CloneFont(
                        tree.Appearance.Row.Font,
                        fontFamily);

                tree.Appearance.HeaderPanel.Font =
                    CloneFont(
                        tree.Appearance.HeaderPanel.Font,
                        fontFamily);

                tree.Appearance.FocusedRow.Font =
                    CloneFont(
                        tree.Appearance.FocusedRow.Font,
                        fontFamily);

                tree.Appearance.Row.Options.UseFont = true;
                tree.Appearance.HeaderPanel.Options.UseFont = true;
                tree.Appearance.FocusedRow.Options.UseFont = true;
            }

            //----------------------------------------
            // NavBar
            //----------------------------------------

            NavBarControl nav =
                control as NavBarControl;

            if (nav != null)
            {
                nav.Appearance.GroupHeader.Font =
                    CloneFont(
                        nav.Appearance.GroupHeader.Font,
                        fontFamily);

                nav.Appearance.Item.Font =
                    CloneFont(
                        nav.Appearance.Item.Font,
                        fontFamily);

                nav.Appearance.GroupHeader.Options.UseFont = true;
                nav.Appearance.Item.Options.UseFont = true;
            }

            //----------------------------------------
            // LayoutControl
            //----------------------------------------

            LayoutControl layout =
                control as LayoutControl;

            if (layout != null)
            {
                layout.Appearance.Control.Font =
                    CloneFont(
                        layout.Appearance.Control.Font,
                        fontFamily);

                layout.Appearance.Control.Options.UseFont = true;
            }
            //----------------------------------------
            // ProgressPanel
            //----------------------------------------
            ProgressPanel progressPanel =
                control as ProgressPanel;
            if (progressPanel != null) {
                progressPanel.Appearance.Font =
                    CloneFont(
                        progressPanel.Appearance.Font,
                        fontFamily);
                progressPanel.Appearance.Options.UseFont = true;
            }
            
            WaitForm waitForm = control as WaitForm;
            if (waitForm != null) {
                waitForm.Font =
                    CloneFont(
                        waitForm.Font,
                        fontFamily);
            }

            AccordionControl accordion =
                control as AccordionControl;
            if (accordion != null)
            {
                // Set font for all relevant AccordionControlElementAppearances
                var itemAppearances = accordion.Appearance.Item;
                if (itemAppearances != null)
                {
                    if (itemAppearances.Normal != null)
                    {
                        itemAppearances.Normal.Font =
                            CloneFont(itemAppearances.Normal.Font, fontFamily);
                        itemAppearances.Normal.Options.UseFont = true;
                    }
                    if (itemAppearances.Hovered != null)
                    {
                        itemAppearances.Hovered.Font =
                            CloneFont(itemAppearances.Hovered.Font, fontFamily);
                        itemAppearances.Hovered.Options.UseFont = true;
                    }
                    if (itemAppearances.Pressed != null)
                    {
                        itemAppearances.Pressed.Font =
                            CloneFont(itemAppearances.Pressed.Font, fontFamily);
                        itemAppearances.Pressed.Options.UseFont = true;
                    }
                    if (itemAppearances.Disabled != null)
                    {
                        itemAppearances.Disabled.Font =
                            CloneFont(itemAppearances.Disabled.Font, fontFamily);
                        itemAppearances.Disabled.Options.UseFont = true;
                    }
                }
            }

            //----------------------------------------
            // RichEditControl
            //----------------------------------------

            RichEditControl rich =
                control as RichEditControl;

            if (rich != null)
            {
                rich.Font =
                    CloneFont(
                        rich.Font,
                        fontFamily);
            }

            //----------------------------------------
            // XtraTabControl
            //----------------------------------------

            XtraTabControl tabs =
                control as XtraTabControl;

            if (tabs != null)
            {
                tabs.AppearancePage.Header.Font =
                    CloneFont(
                        tabs.AppearancePage.Header.Font,
                        fontFamily);

                tabs.AppearancePage.Header.Options.UseFont = true;
            }

            //----------------------------------------
            // Children
            //----------------------------------------

            foreach (Control child in control.Controls)
            {
                Apply(
                    child,
                    fontFamily);
            }
        }

        public static void ApplyFontToAllOpenForms(
            string fontFamily)
        {
            foreach (Form form in Application.OpenForms)
            {
                ApplyFont(
                    form,
                    fontFamily);

                form.Refresh();
            }
        }
    }
}