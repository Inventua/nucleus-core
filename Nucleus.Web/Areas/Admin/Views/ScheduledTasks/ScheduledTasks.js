Page.ScheduledTasksEditor = new (_scheduledTasksEditor);
function _scheduledTasksEditor()
{
  this.ScheduledTaskId = '';
  this.RefreshTaskStatusSelector = '';
  this.Url = '';
  this.Start = _start;
  this.Started = false;

  this.RefreshTaskStatusToken = -1;
  //jQuery(Page).on('progress', function (event) { _cancelRefreshTaskStatus(); });
  function _start()
  {
    this.ScheduleRefreshTaskStatus();
    this.Started = true;
  }

  _scheduledTasksEditor.prototype.ScheduleRefreshTaskStatus = function ()
  {
    this.CancelRefreshTaskStatus();

    var me = this;
   
    if (jQuery(this.RefreshTaskStatusSelector).length !== 0)
    {
      this.RefreshTaskStatusToken = window.setTimeout(function () { me.RefreshTaskStatus(); }, 4000);
    }
  }

  _scheduledTasksEditor.prototype.RefreshTaskStatus = function ()
  {
    if (jQuery(this.RefreshTaskStatusSelector).length !== 0)
    {
      Page.LoadPartialContent(null, this.Url, this.RefreshTaskStatusSelector);
      this.ScheduleRefreshTaskStatus();
    }
  }

  _scheduledTasksEditor.prototype.CancelRefreshTaskStatus = function ()
  {
    if (this.RefreshTaskStatusToken !== -1)
    {
      window.clearTimeout(this.RefreshTaskStatusToken);
    }
  }
}