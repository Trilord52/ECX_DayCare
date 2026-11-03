import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PhotoService } from './photo.service';
import { environment } from '../../../environments/environment';

describe('PhotoService', () => {
  let service: PhotoService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PhotoService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(PhotoService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return null when kind or id is missing', (done) => {
    service.load('', '123').subscribe((result) => {
      expect(result).toBeNull();
      done();
    });
  });

  it('should return null on 404 NOT_FOUND response', (done) => {
    service.load('children', 'child-123').subscribe((result) => {
      expect(result).toBeNull();
      done();
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/children/child-123/photo`);
    expect(req.request.method).toBe('GET');
    req.flush(new Blob(), { status: 404, statusText: 'Not Found' });
  });

  it('should return null on 403 FORBIDDEN response', (done) => {
    service.load('children', 'child-other').subscribe((result) => {
      expect(result).toBeNull();
      done();
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/children/child-other/photo`);
    expect(req.request.method).toBe('GET');
    req.flush(new Blob(), { status: 403, statusText: 'Forbidden' });
  });

  it('should return object URL when photo is successfully retrieved', (done) => {
    const fakeBlob = new Blob(['fake-image-bytes'], { type: 'image/jpeg' });
    const spyCreateUrl = spyOn(URL, 'createObjectURL').and.returnValue('blob:http://localhost/mock-url');

    service.load('children', 'child-success').subscribe((result) => {
      expect(result).toBe('blob:http://localhost/mock-url');
      expect(spyCreateUrl).toHaveBeenCalledWith(fakeBlob);
      done();
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/children/child-success/photo`);
    expect(req.request.method).toBe('GET');
    req.flush(fakeBlob);
  });
});

