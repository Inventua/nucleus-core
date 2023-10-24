/*! nucleus-editmode | Nucleus inline editing client-side handlers and behaviours | (c) Inventua Pty Ptd.  www.nucleus-cms.com */
jQuery(Page).on("ready.admin", _setupEditMode);

function _setupEditMode(e, args)
{
  var _sourceModuleId = null;

  // Handle inline content editing
  jQuery('.nucleus-module-editing *[data-inline-edit-route]')
    .prop('contenteditable', 'true')
    .on('input blur', _handleContentUpdate);

  // Attach events for moving modules by drag & drop
  jQuery('.nucleus-move-dragsource')
    .on('dragstart', _handleStartDrag)
    .on('dragend', _handleEndDrag);

  jQuery('.nucleus-move-droptarget')
    .on('dragenter', _handleDragEnter)
    .on('dragover', _handleDragOver)
    .on('dragleave', _handleDragLeave)
    .on('drop', _handleDrop);

  // determine which drop targets should be aligned sideways (because their pane renders items side-by-side)
  jQuery('.nucleus-move-droptarget').each(function (index, item)
  {
    var dropTargetElement = jQuery(item);
    if
    (
      (dropTargetElement.parent().prev().length !== 0 && dropTargetElement.parent().prev().position().top === dropTargetElement.parent().position().top) ||
      (dropTargetElement.parent().next().length !== 0 && dropTargetElement.parent().next().position().top === dropTargetElement.parent().position().top) ||
      (dropTargetElement.prev().length !== 0 && dropTargetElement.prev().position().top === dropTargetElement.position().top) ||
      (dropTargetElement.next().length !== 0 && dropTargetElement.next().position().top === dropTargetElement.position().top)
    )
    {
      dropTargetElement.addClass('sideways');
    }
  });

  /// Handle "move module" drag start.
  /// Set drag-drop properties in the dataTransfer object, save the ID of the module being moved.
  /// show all drop targets except the one attached to the module being dragged (because that drop target would just move the module to 
  /// its current position)
  function _handleStartDrag(event)
  {
    var dragHandleElement = jQuery(event.target);
    dragHandleElement.addClass('dragging');    

    // show all drop targets except the one attached to the module being dragged (because that drop target would just move the module to its current position)
    jQuery('.nucleus-move-droptarget').each(function (index, item)
    {
      var dragTargetElement = jQuery(item);
      var dragHandleContainer = dragHandleElement.parents('.nucleus-module-editing');
      var dragTargetContainer = dragTargetElement.parents('.nucleus-module-editing');

      if
      (
        // don't show drag target if it is the one attached to the module being dragged
        (!dragTargetContainer.is(jQuery(event.target).parents('.nucleus-module-editing')))
        &&
        // don't show drag target if it is the special drag target at the end of the pane (dragTargetContainer.length === 0, because the special drag targets
        // do not have a .nucleus-module-editing parent), and the module being dragged is already the last module in the(same) pane
        (!(dragTargetContainer.length === 0 && dragTargetElement.parent().is(dragHandleContainer.parent()) && dragTargetElement.is(':last-child')))
      )
      {
        dragTargetElement.addClass('show');
      }
    });

    event.originalEvent.dataTransfer.effectAllowed = 'move';
    _sourceModuleId = jQuery(event.target).attr('data-mid');
  }

  /// Handle the event which is triggered when a drag source icon is initially dragged over a drag target.
  /// Set drag-active css class on the drag target so that CSS can provide visual indicators that the drag target
  /// is valid.  
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

  /// Handle the event which is triggered as a drag source icon is moved over a drag target.
  /// Call event.preventDefault() to signal to the browser that the drag-drop operation is valid.
  function _handleDragOver(event)
  {
    var targetmid = jQuery(event.target).attr('data-mid');

    if (_sourceModuleId !== null && _sourceModuleId !== targetmid)
    {
      event.preventDefault();
    }
  }

  /// Handle the event which is triggered when a drag source icon is moved outside of a drag target.
  /// Remove the drag-active CSS class in order to no longer provide visual indicators that the drag target is valid.
  function _handleDragLeave(event)
  {
    jQuery(event.target).removeClass('drag-active');
  }

  /// Handle the event which is raised when a drag-drop operation is completed.
  /// Hide drag targets.
  function _handleEndDrag(event)
  {
    jQuery(event.target).removeClass('dragging');
    jQuery('.nucleus-move-droptarget').removeClass('show');
    _sourceModuleId = null;
  }

  /// Handle a "drop" event
  /// Call Nucleus to execute the "move module" operation.  Reload the page after the module has been moved.
  function _handleDrop(event)
  {
    var targetpane = jQuery(event.target).attr('data-pane-name');
    var targetmid = jQuery(event.target).attr('data-mid');
   
    if (targetpane !== null && _sourceModuleId !== null)
    {
      var url = document.baseURI + 'admin/pages/movemoduleto?mid=' + _sourceModuleId;
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

  /// This function is called after a user edits inline-editable content.
  /// Send updated content to Nucleus.
  var _handleContentUpdateTimeout = -1;
  function _handleContentUpdate(event)
  {
    if (_handleContentUpdateTimeout > 0)
    {
      window.clearTimeout(_handleContentUpdateTimeout);
    }

    _handleContentUpdateTimeout = window.setTimeout(_commitContentUpdate, 1000, this);
  }

  function _commitContentUpdate(element)
  {
    var url = jQuery(element).attr("data-inline-edit-route");
    var content = new FormData();
    content.append('value', jQuery(element).html());

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