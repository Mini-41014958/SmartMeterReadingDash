async function exportCompleteDashboard() {

    const button =  document.getElementById( "btnExportCompleteDashboard" );

    try
    {

        if (!button)
        {
            throw new Error("Export button not found." );
        }

        button.disabled = true;

        button.innerHTML =  '<span class="spinner-border spinner-border-sm"></span> Exporting...';


        const readingMonth =getReadingMonth();


        if (!readingMonth)
        {
            throw new Error("Reading month is not available." );
        }


        const response = await fetch(`/api/DashboardExportApi/export-report?readingMonth=${encodeURIComponent( readingMonth)}`,
                {
                    method: "GET",

                    credentials: "same-origin",

                    cache: "no-store",

                    headers: {
                        "Accept":
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    }
                }
            );

        if (response.status === 401)
        {

            throw new Error( "Your session has expired. Please login again." );
        }


        if (response.status === 403)
        {

            throw new Error( "You are not authorized to export dashboard data." );
        }

        if (!response.ok)
        {

            let errorMessage = "Export failed.";

            try
            {

                const errorData =  await response.json();

                errorMessage = errorData?.message || errorMessage;

            }
            catch
            {

                const errorText = await response.text();

                if (errorText)
                {
                    errorMessage = errorText;
                }
            }

            throw new Error(errorMessage);
        }


        const blob = await response.blob();


        if (!blob || blob.size === 0)
        {

            throw new Error("Export returned an empty file." );
        }

        const url = window.URL.createObjectURL(blob);

        const a = document.createElement("a");

        a.href = url;

        a.download = `Dashboard_${readingMonth}.xlsx`;


        document.body.appendChild(a);

        a.click();

        a.remove();


        window.URL.revokeObjectURL(url);
    }
    catch (error)
    {

        console.error( "Dashboard export error:",  error );

        alert( "Failed to export dashboard: " + error.message);

    }
    finally
    {

        if (button)
        {

            button.disabled = false;

            button.innerHTML = '<i class="bi bi-file-earmark-excel"></i> Export Dashboard';
        }
    }
}