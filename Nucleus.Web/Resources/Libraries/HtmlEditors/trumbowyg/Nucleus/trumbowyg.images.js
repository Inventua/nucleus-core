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
      ico: 'insert-image',
      fn: function ()
      {
        var $modal = editor.openModal(
          editor.lang.insertNucleusImage,
          [
            '<form class="nucleus-fileselectorform"><div class="nucleus-flex-fields nucleus-fileselector"></div></form>',
            '<div class="nucleus-button-panel"><button class="btn btn-primary insert-image">Insert Image</button><button class="btn btn-secondary cancel-insert-image">Cancel</button></div>',
          ], false);
        
        buildModal(editor, '.nucleus-fileselector');
        
        // Button event handlers
        jQuery('.insert-image').on('click', function ()
        {
          var imgSrc = jQuery('.file-link').html();
          editor.restoreRange();
          editor.execCmd('insertHTML', '<img src="' + imgSrc + '"/>');
          
          editor.closeModal();
        });
                
        jQuery('.cancel-insert-image').on('click', function ()
        {
          editor.closeModal();
        });

      }
    }
  }

  function buildModal(editor, targetSelector)
  {
    var url = document.baseURI + 'User/FileSelector/Index?pattern=(.gif)|(.png)|(.jpg)|(.jpeg)|(.bmp)|(.webp)&showSelectAnother=false&applicationAbsoluteUrl=false';
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
        insertNucleusImage: 'Insert Image'
      }
    },
    // Register plugin in Trumbowyg
    plugins: {
      insertNucleusImage: {
        // Code called by Trumbowyg core to register the plugin
        init: function (trumbowyg)
        {
          // Fill current Trumbowyg instance with the plugin default options
          trumbowyg.o.plugins.images = $.extend(true, {},
            defaultOptions,
            trumbowyg.o.plugins.insertNucleusImage || {}
          );

          // If the plugin is a button
          trumbowyg.addBtnDef('insertNucleusImage', buildButtonDef(trumbowyg));
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