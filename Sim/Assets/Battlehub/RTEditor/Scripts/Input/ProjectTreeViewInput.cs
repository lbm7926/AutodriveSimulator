using UnityEngine;

namespace Battlehub.RTEditor
{
    public class ProjectTreeViewInput : BaseViewInput<ProjectTreeView>
    {
        protected override void UpdateOverride()
        {
            base.UpdateOverride();

            if(DeleteAction())
            {
                View.DeleteSelectedFolders();
            }

            if (SelectAllAction())
            {
                View.SelectAll();
            }
        }
    }
}
