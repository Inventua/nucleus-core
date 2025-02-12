﻿/*! nucleus-editmode | Nucleus inline editing client-side handlers and behaviours | (c) Inventua Pty Ptd.  www.nucleus-cms.com */
jQuery(Page).on("ready.admin", _setupEditMode);

function _setupEditMode(e, args)
{
  var _sourceModuleId = null;

  // Handle inline content editing
  jQuery('.nucleus-module-editing *[data-inline-edit-route]')
    .prop('contenteditable', 'true')
    .on('input blur', _handleContentUpdate);
  jQuery('.nucleus-module-editing *[data-inline-edit-route][data-inline-edit-mode = "MultiLineText"]')
    .on('keydown', _multiLineText_keydown)
    .on('keyup', _editableContent_keyup)
    .on('paste', _multiLineText_paste);
  jQuery('.nucleus-module-editing *[data-inline-edit-route][data-inline-edit-mode = "SingleLineText"]')
    .on('keydown', _singleLineText_keydown)
    .on('keyup', _editableContent_keyup)
    .on('paste', _singleLineText_paste);

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
    var parentElement = dropTargetElement.parent();
    if
    (
      (parentElement.prev().length !== 0 && parentElement.prev().position().top === parentElement.position().top) ||
      (parentElement.next().length !== 0 && parentElement.next().position().top === parentElement.position().top) ||
      (dropTargetElement.prev().length !== 0 && dropTargetElement.prev().position().top === dropTargetElement.position().top) ||
      (dropTargetElement.next().length !== 0 && dropTargetElement.next().position().top === dropTargetElement.position().top)
    )
    {
      if (_isRenderedHorizontal(parentElement))
      {
        dropTargetElement.addClass('sideways');
      }
    }
  });

  function _isRenderedHorizontal(element)
  {
    if (element.css('display') === 'flex' && element.css('flex-direction') === 'row') return true;
    if (element.css('display') === 'inline-flex' && element.css('flex-direction') === 'row') return true;
    if (element.is('td')) return true;
    if (element.css('float') === 'left') return true;

    return false;
  }

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

  /// prevent new lines and some common HTML operations in text-only (single line) editable content.  The _commitContentUpdate function
  /// is the main point where we remove HTML from plain-text content, this function is simply to improve the user experience by avoiding 
  /// cases where the newline or HTML styles appear and then disappear a second later when _commitContentUpdate is called.
  function _singleLineText_keydown(event)
  {
    var suppressKeyCodes = [13];
    var suppressCtrlCodes = [66, 98, 73, 105, 65, 117];  // b,B,i,I,u,U (ctrl-b, ctrl-i, ctrl-u set bold/italic/underline)
    return (!suppressKeyCodes.includes(event.keyCode) && (!event.ctrlKey || !suppressCtrlCodes.includes(event.keyCode)));
  }

  function _singleLineText_paste(event)
  {
    event.preventDefault();
    content = (event.originalEvent || event).clipboardData?.getData('text/plain');

    if (content)
    {
      var range = window.getSelection().getRangeAt(0);
      if (range)
      {
        range.deleteContents();
        range.insertNode(document.createTextNode(content));
        window.getSelection().collapseToEnd();
      }
    }
  }

  // prevent contenteditable controls from triggering button clicks, etc when the space character is entered
  function _editableContent_keyup(event)
  {
    if (event.keyCode === 32)
    {
      event.stopImmediatePropagation();
    }
  }

  /// prevent some common HTML operations in text-only (multi line) editable content but allow newlines.  The _commitContentUpdate function
  /// is the main point where we remove HTML from plain-text content, this function is simply to improve the user experience by avoiding 
  /// cases where the newline or HTML styles appear and then disappear a second later when _commitContentUpdate is called.
  function _multiLineText_keydown(event)
  {
    var suppressCtrlCodes = [66, 98, 73, 105, 65, 117];  // b,B,i,I,u,U (ctrl-b, ctrl-i, ctrl-u set bold/italic/underline)

    if (event.keyCode === 13)
    {
      /// handle new lines in multi-line content.  This is to standardize browser behavior, as some browsers use DIV elements
      /// and other browsers use BR elements.
      var range = window.getSelection().getRangeAt(0);
      if (range)
      {
        range.deleteContents();
        range.insertNode(document.createElement('br'));
        window.getSelection().collapseToEnd();
      }
      return false;
    }
    else
    {
      return (!event.ctrlKey || !suppressCtrlCodes.includes(event.keyCode));
    }
  }

  /// paste plain text into text-only contenteditable controls
  function _multiLineText_paste(event)
  {
    event.preventDefault();
    content = (event.originalEvent || event).clipboardData?.getData('text/plain');

    if (content)
    {
      var range = window.getSelection().getRangeAt(0);
      if (range)
      {
        range.deleteContents();
        var template = document.createElement('template');
        template.innerHTML = content.replaceAll('\n', '<br>');
        for (var count = template.content.childNodes.length - 1; count >= 0; count--)
        {
          range.insertNode(template.content.childNodes[count]);
        }
        window.getSelection().collapseToEnd();
      }
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
    var control = jQuery(element);
    var url = control.attr("data-inline-edit-route");
    var mode = control.attr('data-inline-edit-mode');

    var content;

    if (typeof mode === 'unspecified' || mode === null) return;

    if (mode === "SingleLineText" || mode === "MultiLineText")
    {
      // remove html from the content
      var textValue = element.innerText;
      var content = element.innerHTML
        .replaceAll(/<br([^>]*)>/gi, '\n')
        .replaceAll(/&nbsp;/gi, '\u00A0');

      if (content !== textValue)
      {
        // get caret position
        var range = window.getSelection().getRangeAt(0);
        var selectionStart;
        var selectionLength;

        if (range && range.commonAncestorContainer.data && element.childNodes.length > 0)
        {
          if (range.commonAncestorContainer.data !== textValue)
          {
            selectionStart = control.text().indexOf(range.commonAncestorContainer.data);
            selectionFinish = selectionStart + range?.commonAncestorContainer.data.length;
          }
          else
          {
            selectionStart = range.startOffset;
            selectionFinish = range.endOffset;
          }
        }

        // remove html
        var template = document.createElement('div');
        template.innerHTML = content.replaceAll('<br>', '\n');
        content = template.innerText;
        control.html(template.innerText.replaceAll('\n', '<br>'));

        // restore caret position
        if (selectionStart && selectionStart >= 0)
        {
          var newRange = document.createRange();
          newRange.setStart(element.childNodes[0], selectionStart);
          newRange.setEnd(element.childNodes[0], selectionStart);

          window.getSelection().removeAllRanges();
          window.getSelection().addRange(newRange);
        }
      }
    }
    else
    {
      content = control.html();
    }

    var formContent = new FormData();
    formContent.append('value', content);

    jQuery.ajax({
      url: url,
      async: true,
      method: 'POST',
      enctype: 'multipart/form-data',
      data: formContent,
      headers: { 'Accept': 'application/json, */*' },
      contentType: false,
      processData: false,
      success: function (data, status, request)
      {

      },
      error: function (request, status, message)
      {
        console.error(message);
      }
    })
  }
}