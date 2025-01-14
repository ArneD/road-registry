import apiClient, {AxiosHttpApiClient} from "./api-client";
import RoadRegistry from "@/types/road-registry";
import RoadRegistryExceptions from "@/types/road-registry-exceptions";
import axios from "axios";
import { trimEnd } from 'lodash';
import { featureToggles, API_OLDENDPOINT } from "@/environment";

const apiEndpoint = trimEnd(featureToggles.useDirectApiCalls ? API_OLDENDPOINT : '/roads', '/');

export const BackOfficeApi = {
  ChangeFeed: {
    getHead: async (maxEntryCount: number): Promise<RoadRegistry.GetHeadApiResponse> => {
      const path = `${apiEndpoint}/v1/changefeed/head`;
      const response = await apiClient.get<RoadRegistry.GetHeadApiResponse>(path, { maxEntryCount });
      return response.data as RoadRegistry.GetHeadApiResponse;
    },
    getContent: async (id: number) => {
      const path = `${apiEndpoint}/v1/changefeed/entry/${id}/content`;
      const response = await apiClient.get<RoadRegistry.ChangeFeedContent>(path);
      return response.data;
    },
    getNext: async (afterEntry?: number, maxEntryCount?: number) => {
      const path = `${apiEndpoint}/v1/changefeed/next`;
      const response = await apiClient.get<RoadRegistry.GetHeadApiResponse>(path, { afterEntry, maxEntryCount });
      return response.data;
    },
    getPrevious: async (beforeEntry?: number, maxEntryCount?: number) => {
      const path = `${apiEndpoint}/v1/changefeed/previous`;
      const response = await apiClient.get<RoadRegistry.GetHeadApiResponse>(path, { beforeEntry, maxEntryCount });
      return response.data;
    },
  },
  Downloads: {
    getForEditor: async () => {
      const path = `${apiEndpoint}/v1/download/for-editor`;
      await apiClient.download("application/zip", "wegenregister.zip", path, "GET");
    },
    getForProduct: async (date: string) => {
      const path = `${apiEndpoint}/v1/download/for-product/${date}`;
      await apiClient.download("application/zip", `wegenregister-${date}.zip`, path, "GET");
    },
  },
  Uploads: {
    upload: async (file: string | Blob, filename: string): Promise<number> => {
      const path = `${apiEndpoint}/v1/upload`;
      const data = new FormData();
      data.append("archive", file, filename);
      const response = await apiClient.post(path, data);
      return response.status;
    },
    uploadFeatureCompare: async (file: string | Blob, filename: string): Promise<number> => {
      const path = `${apiEndpoint}/v1/upload/fc`;
      const data = new FormData();
      data.append("archive", file, filename);
      const response = await apiClient.post(path, data);
      return response.status;
    },
    download: async (identifier: string): Promise<void> => {
      const path = `${apiEndpoint}/v1/upload/${identifier}`;
      await apiClient.download("application/zip", `${identifier}.zip`, path, "GET");
    },
  },
  Extracts: {
    download: async (downloadid: string) => {
      const path = `${apiEndpoint}/v1/extracts/download/${downloadid}`;
      await apiClient.download("application/zip", `${downloadid}.zip`, path, "GET");
    },
    upload: async (downloadid: string, file: string | Blob, filename: string) => {
      const path = `${apiEndpoint}/v1/extracts/download/${downloadid}/uploads`;
      const data = new FormData();
      data.append(downloadid, file, filename);
      const response = await apiClient.post<RoadRegistry.UploadExtractResponseBody>(path, data);
      return response.data;
    },
    getUploadStatus: async (uploadid: string): Promise<{ status: string }> => {
      const path = `${apiEndpoint}/v1/extracts/upload/${uploadid}/status`;
      const response = await apiClient.get<{ status: string }>(path);
      return response.data;
    },
    postDownloadRequest: async (
      downloadRequest: RoadRegistry.DownloadExtractRequest
    ): Promise<RoadRegistry.DownloadExtractResponse> => {
      const path = `${apiEndpoint}/v1/extracts/downloadrequests`;
      const response = await apiClient.post<RoadRegistry.DownloadExtractResponse>(path, downloadRequest);
      return response.data;
    },
    postDownloadRequestByContour: async (
      downloadRequest: RoadRegistry.DownloadExtractByContourRequest
    ): Promise<RoadRegistry.DownloadExtractResponse> => {
      const path = `${apiEndpoint}/v1/extracts/downloadrequests/bycontour`;
      const response = await apiClient.post<RoadRegistry.DownloadExtractResponse>(path, downloadRequest);
      return response.data;
    },
    postDownloadRequestByFile: async (
        downloadRequest: RoadRegistry.DownloadExtractByFileRequest
      ): Promise<RoadRegistry.DownloadExtractResponse> => {
        const path = `${apiEndpoint}/v1/extracts/downloadrequests/byfile`;

        const data = new FormData();
        data.append("description", downloadRequest.description);
        data.append("isInformative", downloadRequest.isInformative.toString());
        downloadRequest.files.forEach(file => {
            data.append("files", file, file.name);
        })        

        try {
          const response = await apiClient.post<RoadRegistry.DownloadExtractResponse>(path, data);
        return response.data;
      } catch (exception) {
          if (axios.isAxiosError(exception)) {
              const response = exception?.response;
              if (response && response.status === 400) { // HTTP Bad Request
                  const error = response?.data as RoadRegistry.PerContourErrorResponse;
                  throw new RoadRegistryExceptions.RequestExtractPerContourError(error);
              }
          }

          throw new Error('Unknown error');
      }
      },
    postDownloadRequestByNisCode: async (
      downloadRequest: RoadRegistry.DownloadExtractByNisCodeRequest
    ): Promise<RoadRegistry.DownloadExtractResponse> => {
      const path = `${apiEndpoint}/v1/extracts/downloadrequests/byniscode`;
      const response = await apiClient.post<RoadRegistry.DownloadExtractResponse>(path, downloadRequest);
      return response.data;
    },
  },
  Information: {
    getInformation: async (): Promise<RoadRegistry.RoadNetworkInformationResponse> => {
      const path = `${apiEndpoint}/v1/information`;
      const response = await apiClient.get<RoadRegistry.RoadNetworkInformationResponse>(path);
      return response.data;
    },
    postValidateWkt: async (wkt: String): Promise<RoadRegistry.ValidateWktResponse> => {
      const path = `${apiEndpoint}/v1/information/validate-wkt`;
      const response = await apiClient.post(path, { contour: wkt });
      return response.data;
    },
  },
  Security: {
    getInfo: async (): Promise<RoadRegistry.SecurityInfo> => {
      const path = `${apiEndpoint}/v1/security/info`;
      const response = await apiClient.get<RoadRegistry.SecurityInfo>(path);
      return response.data;
    },
    getExchangeCode: async (code: string, verifier: string, redirectUri: string): Promise<string> => {
      const path = `${apiEndpoint}/v1/security/exchange?code=${code}&verifier=${verifier}&redirectUri=${redirectUri}`;
      const response = await apiClient.get(path);
      return response.data;
    },
    userIsAuthenticated: async (): Promise<boolean> => {
      const apiClient = new AxiosHttpApiClient({
        noRedirectOnUnauthorized: true,
      });
      const path = `${apiEndpoint}/v1/security/user`;
      try {
        await apiClient.get<void>(path);
        return true;
      } catch (err) {
        return false;
      }
    },
  }
};
export default BackOfficeApi;
