(function ($)
{
  jQuery.fn.CopilotViewer = function (conf)
  {
    var options = jQuery.extend({
      url: null,
      interval: 2000
    }, conf);

    var element = jQuery(this);
    var started = false;
    var refreshTaskStatusToken = -1;

    // public functions
    jQuery.fn.CopilotViewer.Start = _start;

    // private functions   
    function _start()
    {
      _scheduleRefresh();
      started = true;
    };

    function _scheduleRefresh()
    {
      _cancelRefreshTaskStatus();
      refreshTaskStatusToken = window.setTimeout(_refreshTaskStatus, options.interval);
    };

    function _refreshTaskStatus()
    {
      var form = element.parents('form').first();

      jQuery.ajax({
        url: options.url,
        async: true,
        method: 'POST',
        enctype: form.attr('enctype'),
        data: (form.attr('enctype') === 'multipart/form-data') ? new FormData(form[0]) : form.serialize(),
        headers: { 'Accept': 'application/json, */*' },
        processData: (form.attr('enctype') === 'multipart/form-data') ? false : true,
        contentType: (form.attr('enctype') === 'multipart/form-data') ? false : 'application/x-www-form-urlencoded; charset=UTF-8',
        success: function (data, status, request)
        {
          if (status != 'success' || typeof (data) === 'undefined' || typeof (data.responses) === 'undefined' || data.responses.length === 0)
          {
            _scheduleRefresh();
          }
          else
          {
            _render(form, data);
            
          }
        },
        error: function (request, status, message)
        {
          _scheduleRefresh();
        },
      });
    };

    function _render(form, data)
    {
      form.find('.copilot-question-input').val('');
      form.find('.copilot-watermark').val(data.watermark);

      var responsesElement = form.siblings('.copilot-responses');
      for (const response of data.responses)
      {
        var citationsElement = jQuery('<div class="copilot-citations d-none"></div>');
        for (const citation of data.citations)
        {
          var citationElement = jQuery('<div data-id="' + citation.id + '" data-title="' + citation.name + '">' + citation.text + '</div>');
          citationsElement.append(citationElement);
        }

        var responseElement = jQuery('<div class="' + response.type + '">' + response.text + '</div>');

        if (citationsElement.children.length > 0)
        {
          responseElement.append(citationsElement);
        }

        responsesElement.append(responseElement);
        responseElement[0].scrollIntoView({ behavior: 'smooth', block: 'end' });
      }

      

      _scheduleRefresh();
    }

    function _cancelRefreshTaskStatus()
    {
      if (refreshTaskStatusToken !== -1)
      {
        window.clearTimeout(refreshTaskStatusToken);
      }
    };

    function _showCitation(event)
    {
      var citationid = jQuery(this).attr('href');
      var citationElement = jQuery(this).parents('.copilot-answer').find('.copilot-citations [data-id="' + citationid + '"]');
      if (citationElement.length !== 0)
      {
        event.preventDefault();
        var dialog = jQuery(this).parents('.copilot-wrapper').find('.copilot-citation-popup');
        dialog.find('.modal-title').html(citationElement.attr('data-title'));
        dialog.find('.modal-body').html(citationElement.text());
        var popup = new bootstrap.Modal(dialog);
        popup.show();
      }
    }

    return this.each(function ()
    {
      jQuery(this).parents('.copilot-wrapper').on('click', 'a', _showCitation);
    });
  };
}(jQuery));