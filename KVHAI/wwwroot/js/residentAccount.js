$(document).ready(function () {

    //event listerner
    $(document).on('change', '#toggleSwitch', function () {
        var toggle = $('#toggleSwitch').prop('checked');
        var lblSwitch = $('#lblSwitch');

        if (toggle) {
            lblSwitch.html('<b>ACTIVE</b>');
            lblSwitch.css('background-color', '#22c55e');
        }
        else {
            lblSwitch.html('<b>PENDING</b>');
            lblSwitch.css('background-color', '#ef4444');
        }

        respagination();
    });

    $(document).on('click', '.btn-res-edit', function () {
        var res_ID = $(this).data('id');
        var _status = "true";
        var _data = { res_id: res_ID, status:_status };
        if (res_ID) {
            $.ajax({
                type: 'POST',
                url: '/adminaccount/UpdateStatus',
                data: _data,
                success: function (response) {
                    toastr.success("Resident status was updated");
                    var result = $(response).find('#res-tableData').html();
                    $('#res-tableData').html(result);
                },
                error: function (xhr, status, err_m) {
                    toastr.error(xhr.responseText);
                    if (xhr.status === 404) {
                        toastr.error("Resource not found");
                    } else if (xhr.status === 500) {
                        toastr.error("Server error. Please try again later.");
                    } else {
                        toastr.error(xhr.responseText || "An unknown error occurred");
                    }
                }
            });
        }
    })

    $(document).on('click', '.btn-res-delete', function () {
        var res_ID = $(this).data('id');
        var _status = "null";
        var _data = { res_id: res_ID, status: _status };
        if (res_ID) {
            $.ajax({
                type: 'POST',
                url: '/adminaccount/UpdateStatus',
                data: _data,
                success: function (response) {
                    toastr.success("Resident status was updated");
                    var result = $(response).find('#res-tableData').html();
                    $('#res-tableData').html(result);
                },
                error: function (xhr, status, err_m) {
                    toastr.error(xhr.responseText);
                    if (xhr.status === 404) {
                        toastr.error("Resource not found");
                    } else if (xhr.status === 500) {
                        toastr.error("Server error. Please try again later.");
                    } else {
                        toastr.error(xhr.responseText || "An unknown error occurred");
                    }
                }
            });
        }
    })

    //event listener
    $(document).on('change', '#res-search', function () {
        respagination();
    })

    $(document).on('click', '.respagination', function (event) {
        event.preventDefault();
        //const page = parseInt($(this).text(), 10);
        var respage = parseInt($(this).data('respagination'), 10);
        respagination(respage);
    });

    function hidePreloader() {
        const loader = $('#loader');
        setTimeout(function () {
            $('#loader').addClass('d-none');
        }, 2000);

    }
    function showPreloader() {
        const loader = $('#loader');
        loader.removeClass('d-none');
        setTimeout(function () {
            $('#loader').addClass('d-none');
        }, 2000);
    }

    /*   NAVBAR    */
    $('section').hide(); // Hide all sections initially


    $('#staff').show(); // Show the default section (Staff)


    $('.link-header').click(function (e) { // Handle click events on header links
        e.preventDefault();


        $('.link-header').removeClass('active');// Remove 'active' class from all links


        $(this).addClass('active');// Add 'active' class to clicked link


        var targetSection = $(this).attr('href');// Get the target section from the href attribute

        console.log(targetSection)

        $('section').hide(); // Hide all sections


        $(targetSection).show(); // Show the target section
        /* END  NAVBAR    */


        /*IMAGE GET METHOD*/
        $('.load-image').on('click', function () {
            var residentId = $(this).siblings('#res_id').val();
            if (residentId) { // Check if residentId has a value
                $('#staticBackdrop').data('resident-id', residentId); // Store the residentId in a data attribute of the modal

                // Show preloader before triggering the modal to open
                // Trigger the modal to open
                $('#staticBackdrop').modal(showPreloader(), 'show');
            } else {
                alert('No resident ID found.');
            }
        });
        /*END IMAGE METHOD*/

        // Add this new event listener
        $('#staticBackdrop').on('shown.bs.modal', function (e) { // changed to 'shown.bs.modal'
            var modal = $(this);
            var residentId = modal.data('resident-id');

            if (residentId) {
                $.ajax({
                    type: 'GET',
                    url: '/adminaccount/GetImageBase64',
                    //url: '@Url.Action("GetImageBase64", "AdminAccount")',
                    data: { id: residentId },
                    success: function (result) {
                        if (result.success) {
                            $('#modalImage').attr('src', 'data:image/jpeg;base64,' + result.imageBase64);
                            $('#modalImage').show();
                        } else {
                            alert('Failed to load image.');
                        }
                        //hidePreloader(); // Hide preloader after the image is successfully loaded
                    },
                    error: function () {
                        alert('An error occurred while loading the image.');
                        hidePreloader(); // Hide preloader if there's an error
                    }
                });
            } else {
                hidePreloader(); // Hide preloader if there's no residentId
                alert('No resident ID available.');
            }
        });

        // Hide preloader when the modal is hidden
        $('#staticBackdrop').on('hidden.bs.modal', function () {
            // Reset modal content when it's closed
            $('#modalImage').attr('src', '').hide();
            $('#imagePreloader').show();
        });
    });

    function respagination(i = 1) {//Yung i is default pero pwedeng ibang letter ilagay dyan [i] lang nilalagay ko
        var _search = $('#res-search').val();
        var _category = $('#res-category').val();
        var _isActive = $('#toggleSwitch').prop('checked');


        var array = {
            search: _search,
            category: _category.toLowerCase(),
            active: _isActive.toString(),
            page_index: i
            
        };

 
        $.ajax({
            url: window.location.origin + '/adminaccount/ResidentPagination',
            type: "POST",
            data: array,
            success: function (response) {
                var result = $(response).find("#res-tableData").html();
                console.log(result);
                $('#res-tableData').html(result)

                var btnEdit = $('.btn-res-edit');
                var btnDelete = $('.btn-res-delete');

                if (_isActive) {
                    $('.btn-res-edit').addClass('disabled');
                    $('.btn-res-delete').addClass('disabled');

                }
                else {
                    $('.btn-res-edit').removeClass('disabled');
                    $('.btn-res-delete').removeClass('disabled');
                }
            },
            error: function (xhr, status, error_m) {
                toastr.error(xhr.responseText);
            }
        });
    }
});