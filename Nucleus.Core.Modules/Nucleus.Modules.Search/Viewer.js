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

    function _showSearchTerm()
    {      
      let url = new URL(location);
      url.searchParams.set("search", jQuery(selectorPrefix + '.search-term').val());
      window.history.replaceState({}, '', url);      
    }

    function _attachSuggestionsEventHandlers()
    {
      /* close suggestions when mouse is clicked elsewhere */
      jQuery(document).on('click', '', function ()
      {
        jQuery(selectorPrefix + '.suggestions-result').removeClass('show');
      });

      /* get updated search suggestions */
      jQuery(selectorPrefix + '.search-term').on('input', function (event)
      {
        /* clear any previous countdowns */
        if (suggestionsTimeout !== -1)
        {
          window.clearTimeout(suggestionsTimeout);
        }

        /* wait half a second before submitting the request, so that if the user is typing, we don't send multiple unwanted requests */
        suggestionsTimeout = window.setTimeout(function (form) { form.submit(); }, 500, jQuery(this).parents('form').first());
        return false;
      });
    }

    function _showSearchSuggestions(event, data)
    {
      if (typeof (data.target) !== 'undefined' && data.target.hasClass('search-suggestions'))
      {
        // position the suggestions drop-down below the search term text box
        try
        {           
          var element = jQuery(selectorPrefix + ' .search-term');
          if (element.length !== 0)
          {
            data.target.css('left', element.offset().left);
            data.target.css('max-width', element.width() * 1.4);
          }
        }
        catch (error)
        {
          console.error(error);
        }

        /*  if the suggestions list overflows the right-hand side of the page, move it back to align to the right of the page */
        data.target.css('margin-left', '');
        let overflowpx = jQuery(window).width() - (data.target.offset().left + data.target.outerWidth(true));

        if (overflowpx < 0)
        {
          data.target.css('margin-left', overflowpx);
        }

        data.target.collapse('show');

        data.target.find('.suggestions-result li').on('click', function (event)
        {
          let textbox = jQuery(this).parents('form').first().find('.search-term');
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

      let form = jQuery(element).parents('form').first();
      if (typeof form.attr('data-resultsurl') !== 'undefined' && form.attr('data-resultsurl') !== '')
      {
        window.location = form.attr('data-resultsurl') + '?search=' + form.find(selectorPrefix + '.search-term').val();
      }
      else
      {
        form.find('button[type=submit').trigger('click');
      }
      return false;
    }

    return this.each(function ()
    {
      selectorPrefix = '._' + options.moduleId + ' ';

      /* do search on ENTER */
      jQuery(options.searchTermSelector).on('keydown', function (event)
      {
        if (event.keyCode === 13)
        {
          return _doSearch(this);
        }
      });


      /* the search-suggestions div isn't rendered if the user has disabled search suggestions by setting the max count to zero */
      if (jQuery(selectorPrefix + '.search-suggestions').length !== 0)
      {
        _attachSuggestionsEventHandlers();
      }

      jQuery(Page).on("ready", function (event, data)
      {
        if (jQuery(data.target).is('.search-suggestions'))
        {
          _showSearchSuggestions(event, data);
        }
        else
        {
          _showSearchTerm();
        }
      });
    });
  }
}(jQuery));
