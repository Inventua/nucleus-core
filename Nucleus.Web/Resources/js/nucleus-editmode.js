/*! nucleus-editmode | Nucleus inline editing client-side handlers and behaviours | (c) Inventua Pty Ptd.  www.nucleus-cms.com */
jQuery(Page).on("ready.admin", _setupEditMode);

function _setupEditMode(e, args)
{
  var _sourceModuleId = null;

  // Handle inline content editing
  jQuery('.nucleus-module-editing *[data-inline-edit-route]')
    .prop('contenteditable', 'true')
    .on('blur', _handleContentUpdate);

  // Handle drag-drop events for moving modules by drag & drop
  jQuery('.nucleus-move-dragsource')
    .on('dragstart', _handleStartDrag)
    .on('dragend', _handleEndDrag);

  jQuery('.nucleus-move-droptarget')
    .on('dragenter', _handleDragEnter)
    .on('dragover', _handleDragOver)
    .on('dragleave', _handleDragLeave)
    .on('drop', _handleDrop);

  function _handleStartDrag(event)
  {
    jQuery(event.target).addClass('dragging');    
    jQuery('.nucleus-move-droptarget').addClass('show');
    event.originalEvent.dataTransfer.effectAllowed = 'move';
    _sourceModuleId = jQuery(event.target).attr('data-mid');
  }

  function _handleDragEnter(event)
  {
    var targetmid = jQuery(event.target).attr('data-mid');

    if (_sourceModuleId !== null && _sourceModuleId !== targetmid)
    {
      jQuery(event.target).addClass('drag-active');
      event.originalEvent.dataTransfer.dropEffect = 'move';
    }
    else
    {
      event.originalEvent.dataTransfer.dropEffect = 'none';
    }
  }

  function _handleDragOver(event)
  {
    var targetmid = jQuery(event.target).attr('data-mid');

    if (_sourceModuleId !== null && _sourceModuleId !== targetmid)
    {
      event.preventDefault();
    }
  }

  function _handleDragLeave(event)
  {
    jQuery(event.target).removeClass('drag-active');
  }

  function _handleEndDrag(event)
  {
    jQuery(event.target).removeClass('dragging');
    jQuery('.nucleus-move-droptarget').removeClass('show');
    _sourceModuleId = null;
  }

  function _handleDrop(event)
  {
    var targetpane = jQuery(event.target).attr('data-pane-name');
    var targetmid = jQuery(event.target).attr('data-mid');
   
    if (targetpane !== null && _sourceModuleId !== null)
    {
      var url = '/admin/pages/movemoduleto?mid=' + _sourceModuleId;
      var content = new FormData();

      content.append('pane', targetpane);

      if (typeof targetmid !== 'undefined')
      {
        content.append('beforeModuleId', targetmid);
      }

      jQuery.ajax({
        url: url,
        async: true,
        method: 'POST',
        enctype: 'multipart/form-data',
        data: content,
        headers: { 'Accept': 'application/json, */*' },
        contentType: false,
        processData: false,
        success: function (data, status, request)
        {
          // reload the page
          window.location.reload(true);
        },
        error: function (request, status, message)
        {

        }
      })
    }    
  }

  // send updated content
  function _handleContentUpdate(event)
  {
    var url = jQuery(this).attr("data-inline-edit-route");
    var content = new FormData();
    content.append('value', jQuery(this).html());

    jQuery.ajax({
      url: url,
      async: true,
      method: 'POST',
      enctype: 'multipart/form-data',
      data: content,
      headers: { 'Accept': 'application/json, */*' },
      contentType: false,
      processData: false,
      success: function (data, status, request)
      {

      },
      error: function (request, status, message)
      {

      }
    })
  }
}