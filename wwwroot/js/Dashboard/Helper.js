let isFirstLoad = true;

function getReadingMonth() {

    if (isFirstLoad) {

        isFirstLoad = false;

        const today = new Date();

        const current =
            today.getFullYear() +
            String(today.getMonth() + 1).padStart(2, "0");

        const previousDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);

        const previous =
            previousDate.getFullYear() +
            String(previousDate.getMonth() + 1).padStart(2, "0");

        return previous + "," + current;
    }

    return $("#readingMonth").val().replace("-", "");
}