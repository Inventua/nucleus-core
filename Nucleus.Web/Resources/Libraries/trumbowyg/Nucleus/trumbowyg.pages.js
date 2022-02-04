(function ($)
{
  'use strict';

  // Plugin default options
  var defaultOptions = {
  };

  // If the plugin is a button
  function buildButtonDef(editor)
  {
    return {
      ico: 'create-link',
      fn: function ()
      {
        //<svg><use xlink:href="#trumbowyg-insert-page"></use></svg>
        var $modal = editor.openModal(
          editor.lang.insertNucleusPage,
          [
            '<form class="nucleus-pageselectorform"><div class="nucleus-flex-fields nucleus-pageselector"></div></form>',
            '<div class="nucleus-button-panel"><button class="btn btn-primary insert-page">Insert Page Link</button><button class="btn btn-secondary cancel-insert-page">Cancel</button></div>',
          ], false);
        
        buildModal(editor, '.nucleus-pageselector');
        
        // Button event handlers
        jQuery('.insert-page').on('click', function ()
        {
          var pageSrc = jQuery('.nucleus-pageselector li > a.selected').attr('data-linkurl');
          var pageText = jQuery('.nucleus-pageselector li > a.selected').html();
          editor.restoreRange();
          editor.execCmd('insertHTML', '<a href="' + pageSrc + '">' + pageText + '</a>' );
          
          editor.closeModal();
        });
                
        jQuery('.cancel-insert-page').on('click', function ()
        {
          editor.closeModal();
        });

      }
    }
  }

  function buildModal(editor, targetSelector)
  {
    var url = '/User/PageSelector/Index';
    jQuery.ajax({
      url: url,
      method: 'GET',
      success: function (data, status, request)
      {
        jQuery(targetSelector).html(data);
      }
    });

    var form = jQuery('.trumbowyg-modal form');
    form.attr('data-target', targetSelector);
    form.attr('action', url);
    form.attr('method', 'POST');
  }

  $.extend(true, $.trumbowyg, {
    // Add some translations
    langs: {
      en: {
        insertNucleusPage: 'Insert Page Link'
      }
    },
    // Register plugin in Trumbowyg
    plugins: {
      insertNucleusPage: {
        // Code called by Trumbowyg core to register the plugin
        init: function (trumbowyg)
        {
          // Fill current Trumbowyg instance with the plugin default options
          trumbowyg.o.plugins.pages = $.extend(true, {},
            defaultOptions,
            trumbowyg.o.plugins.insertNucleusPage || {}
          );

          // If the plugin is a button
          trumbowyg.addBtnDef('insertNucleusPage', buildButtonDef(trumbowyg));
        },
        // Return a list of button names which are active on current element
        tagHandler: function (element, trumbowyg)
        {
          return [];
        },
        destroy: function (trumbowyg)
        {
        }
      }
    }
  })
})(jQuery);