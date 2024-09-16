/*! Plug-in that creates a list of suggestions and search results based on query.  Part of the Nucleus CMS Search module.  (c) Inventua Pty Ptd.  www.nucleus-cms.com */
(function ($)
{
  jQuery.fn.SearchResults = function (conf)
  {
    let options = jQuery.extend({
      moduleId: ''
    }, conf);

    let suggestionsTimeout = -1;
    let selectorPrefix = '';

    function _attachSuggestionsEventHandlers()
    {
      /* Close suggestions when mouse is clicked elsewhere */
      jQuery(document).on('click', '', function ()
      {
        jQuery(selectorPrefix + '.suggestions-result').removeClass('show');
      });

      /* get updated search suggestions */
      jQuery(selectorPrefix + '.search-term').on('input', function (event)
      {
        /* wait half a second before submitting the request, so that if the user is typing, we don't send multiple unwanted requests */
        if (suggestionsTimeout !== -1)
        {
          window.clearTimeout(suggestionsTimeout);
        }

        suggestionsTimeout = window.setTimeout(function (form) { form.submit(); }, 500, jQuery(this).parents('form').first());
        return false;
      });
    }

    function _showSearchSuggestions(event, data)
    {
      if (typeof (data.target) !== 'undefined' && data.target.hasClass('search-suggestions'))
      {
        /*  If suggestions list overflows the right-hand side of the page, move it back to align to the right of the page */
        data.target.css(selectorPrefix + 'margin-left', '');
        let overflowpx = jQuery(window).width() - (data.target.offset().left + data.target.outerWidth(true));

        if (overflowpx < 0)
        {
          data.target.css(selectorPrefix + 'margin-left', overflowpx);
        }

        data.target.collapse('show');

        data.target.find(selectorPrefix + '.suggestions-result li').on('click', function (event)
        {
          let textbox = jQuery(this).parents('form').find(selectorPrefix + '.search-term');
          textbox.val(jQuery(this).attr('title'));
          _doSearch(this);
        });
      }
    }

    function _doSearch(element)
    {
      /* if there is a pending call to get search suggestions, cancel it */
      if (suggestionsTimeout !== -1)
      {
        window.clearTimeout(suggestionsTimeout);
      }

      let form = jQuery(element).parents('form');
      if (typeof form.attr('data-resultsurl') !== 'undefined' && form.attr('data-resultsurl') !== '')
      {
        window.location = form.attr('data-resultsurl') + '?search=' + form.find(selectorPrefix + '.search-term').val();
        return false;
      }
      return true;
    }

    return this.each(function ()
    {
      selectorPrefix = '._' + options.moduleId + ' ';

      /* do search on ENTER */
      jQuery(options.searchTermSelector).on('keydown', function (event)
      {
        if (event.keyCode === 13)
        {
          _doSearch(this);
          return false;
        }
      });


      /* the search-suggestions div isn't rendered if the user has disabled search suggestions by setting the max count to zero */
      if (jQuery(selectorPrefix + '.search-suggestions').length !== 0)
      {
        _attachSuggestionsEventHandlers();
      }

      jQuery(Page).on("ready", function (event, data)
      {
        _showSearchSuggestions(event, data);
      });
    });
  }
}(jQuery));
