$(document).ready(function () {

    //event listerner
    $(document).on('change', '#toggleSwitch', function () {
        var toggle = $('#toggleSwitch').prop('checked');
        var lblSwitch = $('#lblSwitch');

        if (toggle) {
            lblSwitch.html('<b>VERIFIED</b>');
            lblSwitch.css('background-color', '#22c55e');
        }
        else {
            lblSwitch.html('<b>NOT VERIFIED</b>');
            lblSwitch.css('background-color', '#ef4444');
        }

        respagination();
    });

    $(document).on('click', '.btn-res-edit', function () {
        var addresID = $(this).data('id');
        var _status = "true";
        var _data = { addrID: addresID, status:_status };
        if (addresID) {
            $.ajax({
                type: 'POST',
                url: '/ResidentAddress/UpdateStatus',
                data: _data,
                success: function (response) {
                    toastr.success("Resident address was approved");
                    var result = $(response).find('#res-tableData').html();
                    $('#res-tableData').html(result);
                },
                error: function (xhr, status, err_m) {
                    toastr.error(xhr.responseText);
                    //if (xhr.status === 404) {
                    //    toastr.error("Resource not found");
                    //} else if (xhr.status === 500) {
                    //    toastr.error("Server error. Please try again later.");
                    //} else {
                    //    toastr.error(xhr.responseText || "An unknown error occurred");
                    //}
                }
            });
        }
    })

    $(document).on('click', '.btn-res-delete', function () {
        var addressID = $(this).data('id');
        var _status = "null";
        var _data = { addrID: addressID, status: _status };
        if (addressID) {
            $.ajax({
                type: 'POST',
                url: '/ResidentAddress/UpdateStatus',
                data: _data,
                success: function (response) {
                    toastr.success("Resident address was disapprove");
                    var result = $(response).find('#res-tableData').html();
                    $('#res-tableData').html(result);
                },
                error: function (xhr, status, err_m) {
                    toastr.error(xhr.responseText);
                    //if (xhr.status === 404) {
                    //    toastr.error("Resource not found");
                    //} else if (xhr.status === 500) {
                    //    toastr.error("Server error. Please try again later.");
                    //} else {
                    //    toastr.error(xhr.responseText || "An unknown error occurred");
                    //}
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
        console.log(respage);
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

    /*IMAGE GET METHOD*/
    $(document).on('click', '.load-image', function () {
        //var addressID = $(this).siblings('#res_id').val();
        var addressID = $(this).data('id');

        if (addressID) { // Check if residentId has a value
            $('#staticBackdrop').data('address-id', addressID); // Store the residentId in a data attribute of the modal

            // Show preloader before triggering the modal to open
            // Trigger the modal to open
            showPreloader();
            $('#staticBackdrop').modal('show');
        } else {
            alert('No resident ID found.');
        }
    });
    /*END IMAGE METHOD*/

    // Add this new event listener
    $('#staticBackdrop').on('shown.bs.modal', function (e) { // changed to 'shown.bs.modal'
        var modal = $(this);
        var addressID = modal.data('address-id');

        if (addressID) {
            $.ajax({
                type: 'GET',
                url: '/ResidentAddress/GetImageBase64',
                //url: '@Url.Action("GetImageBase64", "AdminAccount")',
                data: { id: addressID },
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

    function respagination(i = 1) {//Yung i is default pero pwedeng ibang letter ilagay dyan [i] lang nilalagay ko
        var _search = $('#res-search').val();
        var _category = $('#res-category').val();
        var _isActive = $('#toggleSwitch').prop('checked');
        console.log(`Category = ${_category}, Active = ${_isActive}`);

        var array = {
            search: _search,
            category: _category.toLowerCase(),
            is_verified: _isActive.toString(),
            page_index: i
            
        };

 
        $.ajax({
            url: window.location.origin + '/ResidentAddress/ResidentPagination',
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