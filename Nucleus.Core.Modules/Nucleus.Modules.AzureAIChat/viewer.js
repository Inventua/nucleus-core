jQuery(function() {
  jQuery('.azure-ai-chat .btn-clear').on('click', function () 
  {
    jQuery(this).parents('.azure-ai-chat')
      .first()
      .find('.answers')
      .html('');
  });

  jQuery('.azure-ai-chat').on('success', function (target, data, url, event, status, request)
  {
    var newContent = jQuery(this).find('.next-answer');

    if (newContent.find('.alert-warning').length === 0)
    {
      jQuery(this).find('.chat-question')
        .val('')
        .focus();
    }

    // remove old history hidden inputs
    jQuery(this)
      .find('.answers .history-question, .answers .history-answer')
      .remove();

    jQuery(this).find('.answers').prepend(newContent.children());


  });
});